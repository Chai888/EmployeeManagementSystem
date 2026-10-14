using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Api.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly AuditLogService _auditLogService;
    public AuthService(
    ApplicationDbContext context,
    IConfiguration configuration,
    AuditLogService auditLogService)
    {
        _context = context;
        _configuration = configuration;
        _auditLogService = auditLogService;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Username == request.Username);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(
        "This account has been deactivated.");
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Role = user.Role,
            EmployeeId = user.EmployeeId
        };
    }



    public async Task ChangePasswordAsync(
        int userId,
        ChangePasswordRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "User account not found or inactive.");
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.CurrentPassword);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException(
                "Current password is incorrect.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new InvalidOperationException(
                "New password must be different from the current password.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.NewPassword);

        await _auditLogService.LogAsync(
            user.Id,
            user.Username,
            "PasswordChanged",
            "User",
            user.Id.ToString(),
            "User successfully changed their password.");

        await transaction.CommitAsync();
    }

    private string GenerateJwtToken(User user)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer is not configured.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience is not configured.");

        var expirationMinutes =
            int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };


        if (user.EmployeeId.HasValue)
        {
            claims.Add(
                new Claim("EmployeeId", user.EmployeeId.Value.ToString()));
        }

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}