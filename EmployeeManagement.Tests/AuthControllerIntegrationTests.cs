using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace EmployeeManagement.Tests;

public class AuthControllerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerIntegrationTests(
    CustomWebApplicationFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Auth/change-password",
            new
            {
                currentPassword = "OldPassword123",
                newPassword = "NewPassword123",
                confirmNewPassword = "NewPassword123"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithInvalidToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                "this-is-not-a-valid-jwt-token");

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/change-password",
            new
            {
                currentPassword = "OldPassword123",
                newPassword = "NewPassword123",
                confirmNewPassword = "NewPassword123"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task ChangePassword_WithValidToken_ChangesPasswordSuccessfully()
    {
        // Arrange: create a test user in the test database.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var passwordHasher = new PasswordHasher<User>();

            var user = new User
            {
                Username = "integrationuser",
                Email = "integrationuser@example.com",
                Role = "Employee",
                IsActive = true,
                EmployeeId = null
            };

            user.PasswordHash = passwordHasher.HashPassword(
                user,
                "OldPassword123!");

            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        // Act: log in using the original password.
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                username = "integrationuser",
                password = "OldPassword123!"
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        // Act: change the password using the JWT token.
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        var changeResponse = await _client.PostAsJsonAsync(
    "/api/Auth/change-password",
    new
    {
        currentPassword = "OldPassword123!",
        newPassword = "NewPassword456!",
        confirmNewPassword = "NewPassword456!"
    });

        var changeResponseBody =
            await changeResponse.Content.ReadAsStringAsync();

        Console.WriteLine(
            $"Change password status: {(int)changeResponse.StatusCode}");

        Console.WriteLine(
            $"Change password response: {changeResponseBody}");

        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);

        // The old password should no longer work.
        _client.DefaultRequestHeaders.Authorization = null;

        var oldPasswordResponse = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                username = "integrationuser",
                password = "OldPassword123!"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            oldPasswordResponse.StatusCode);

        // The new password should work.
        var newPasswordResponse = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                username = "integrationuser",
                password = "NewPassword456!"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            newPasswordResponse.StatusCode);
    }

    private async Task<string> CreateUserAndLoginAsync(
    string username,
    string role,
    int? employeeId = null)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var passwordHasher = new PasswordHasher<User>();

            var user = new User
            {
                Username = username,
                Email = $"{username}@example.com",
                Role = role,
                IsActive = true,
                EmployeeId = employeeId
            };

            user.PasswordHash = passwordHasher.HashPassword(
                user,
                "TestPassword123!");

            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                username,
                password = "TestPassword123!"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        return loginResult.Token;
    }

    private async Task<(int DepartmentId, int Employee1Id, int Employee2Id)>
        CreateTwoEmployeesAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var department = new Department
        {
            Name = $"Test Department {Guid.NewGuid():N}"
        };

        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var employee1 = new Employee
        {
            EmployeeNo = $"EMP-{Guid.NewGuid():N}",
            Name = "Test Employee One",
            Email = $"employee1-{Guid.NewGuid():N}@example.com",
            DepartmentId = department.Id,
            Position = "Developer",
            JoinDate = new DateTime(2024, 1, 1),
            IsActive = true
        };

        var employee2 = new Employee
        {
            EmployeeNo = $"EMP-{Guid.NewGuid():N}",
            Name = "Test Employee Two",
            Email = $"employee2-{Guid.NewGuid():N}@example.com",
            DepartmentId = department.Id,
            Position = "Tester",
            JoinDate = new DateTime(2024, 2, 1),
            IsActive = true
        };

        context.Employees.AddRange(employee1, employee2);
        await context.SaveChangesAsync();

        return (department.Id, employee1.Id, employee2.Id);
    }

    [Fact]
    public async Task GetEmployees_AsEmployee_ReturnsForbidden()
    {
        var (_, employee1Id, _) =
            await CreateTwoEmployeesAsync();

        var token = await CreateUserAndLoginAsync(
            $"employee_{Guid.NewGuid():N}",
            "Employee",
            employee1Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                token);

        var response = await _client.GetAsync("/api/Employee");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetOtherEmployee_AsEmployee_ReturnsForbidden()
    {
        var (_, employee1Id, employee2Id) =
            await CreateTwoEmployeesAsync();

        var token = await CreateUserAndLoginAsync(
            $"employee_{Guid.NewGuid():N}",
            "Employee",
            employee1Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                token);

        var response = await _client.GetAsync(
            $"/api/Employee/{employee2Id}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetEmployees_AsAdmin_ReturnsOk()
    {
        await CreateTwoEmployeesAsync();

        var token = await CreateUserAndLoginAsync(
            $"admin_{Guid.NewGuid():N}",
            "Admin");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                token);

        var response = await _client.GetAsync("/api/Employee");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }
}