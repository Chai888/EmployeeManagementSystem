using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Tests;

public class AuthServiceTests
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics
                        .InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IConfiguration CreateConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] =
                "ThisIsATestSecretKeyWithAtLeast32Characters!",
            ["Jwt:Issuer"] = "EmployeeManagement.Tests",
            ["Jwt:Audience"] = "EmployeeManagement.Tests",
            ["Jwt:ExpirationMinutes"] = "60"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private User CreateUser(
        string username = "testuser",
        string password = "Password123!",
        string role = "Employee",
        int? employeeId = 10,
        bool isActive = true)
    {
        var user = new User
        {
            Username = username,
            Email = $"{username}@example.com",
            Role = role,
            EmployeeId = employeeId,
            IsActive = isActive
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            password);

        return user;
    }

    private static AuthService CreateService(
        ApplicationDbContext context)
    {
        var auditLogService = new AuditLogService(context);

        return new AuthService(
            context,
            CreateConfiguration(),
            auditLogService);
    }

    [Fact]
    public async Task LoginAsync_UserDoesNotExist_ThrowsUnauthorizedAccessException()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var request = new LoginRequest
        {
            Username = "unknown",
            Password = "Password123!"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorizedAccessException()
    {
        using var context = CreateContext();

        context.Users.Add(CreateUser(isActive: false));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "Password123!"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_IncorrectPassword_ReturnsNull()
    {
        using var context = CreateContext();

        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "WrongPassword123!"
        };

        var result = await service.LoginAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUserDetails()
    {
        using var context = CreateContext();

        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "Password123!"
        };

        var result = await service.LoginAsync(request);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("testuser", result.Username);
        Assert.Equal("Employee", result.Role);
        Assert.Equal(10, result.EmployeeId);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_TokenContainsExpectedClaims()
    {
        using var context = CreateContext();

        context.Users.Add(CreateUser(
            username: "adminuser",
            role: "Admin",
            employeeId: null));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.LoginAsync(new LoginRequest
        {
            Username = "adminuser",
            Password = "Password123!"
        });

        Assert.NotNull(result);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);

        Assert.Contains(token.Claims, claim =>
            claim.Type == ClaimTypes.NameIdentifier &&
            claim.Value == "1");

        Assert.Contains(token.Claims, claim =>
            claim.Type == ClaimTypes.Name &&
            claim.Value == "adminuser");

        Assert.Contains(token.Claims, claim =>
            claim.Type == ClaimTypes.Role &&
            claim.Value == "Admin");

        Assert.DoesNotContain(token.Claims, claim =>
            claim.Type == "EmployeeId");
    }

    [Fact]
    public async Task ChangePasswordAsync_UserDoesNotExist_ThrowsUnauthorizedAccessException()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ChangePasswordAsync(999, request));
    }

    [Fact]
    public async Task ChangePasswordAsync_InactiveUser_ThrowsUnauthorizedAccessException()
    {
        using var context = CreateContext();

        context.Users.Add(CreateUser(isActive: false));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ChangePasswordAsync(1, request));
    }

    [Fact]
    public async Task ChangePasswordAsync_IncorrectCurrentPassword_ThrowsInvalidOperationException()
    {
        using var context = CreateContext();

        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ChangePasswordAsync(1, request));
    }

    [Fact]
    public async Task ChangePasswordAsync_SamePassword_ThrowsInvalidOperationException()
    {
        using var context = CreateContext();

        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Password123!",
            NewPassword = "Password123!",
            ConfirmNewPassword = "Password123!"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ChangePasswordAsync(1, request));
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_UpdatesPasswordAndCreatesAuditLog()
    {
        using var context = CreateContext();

        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var originalHash = user.PasswordHash;
        var service = CreateService(context);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "NewPassword456!"
        };

        await service.ChangePasswordAsync(1, request);

        var updatedUser = await context.Users.SingleAsync();

        Assert.NotEqual(originalHash, updatedUser.PasswordHash);

        Assert.Equal(
            PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(
                updatedUser,
                updatedUser.PasswordHash,
                "NewPassword456!"));

        var auditLog = await context.AuditLogs.SingleAsync();

        Assert.Equal("PasswordChanged", auditLog.Action);
        Assert.Equal("User", auditLog.EntityType);
        Assert.Equal("1", auditLog.EntityId);

        // Passwords and password hashes must never be recorded in audit details.
        Assert.DoesNotContain("Password123!", auditLog.Details ?? string.Empty);
        Assert.DoesNotContain("NewPassword456!", auditLog.Details ?? string.Empty);
        Assert.DoesNotContain(originalHash, auditLog.Details ?? string.Empty);
    }
}