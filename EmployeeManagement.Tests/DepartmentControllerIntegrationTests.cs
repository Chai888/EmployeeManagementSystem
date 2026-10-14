using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EmployeeManagement.Tests;

public class DepartmentControllerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DepartmentControllerIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDepartments_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/Department");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetDepartments_AsEmployee_ReturnsOk()
    {
        var token = await CreateUserAndLoginAsync(
            $"employee_{Guid.NewGuid():N}",
            "Employee");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await _client.GetAsync("/api/Department");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;
        }
    }

    [Fact]
    public async Task CreateDepartment_AsEmployee_ReturnsForbidden()
    {
        var token = await CreateUserAndLoginAsync(
            $"employee_{Guid.NewGuid():N}",
            "Employee");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await _client.PostAsJsonAsync(
                "/api/Department",
                new CreateDepartmentRequest
                {
                    Name = $"Test Department {Guid.NewGuid():N}"
                });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;
        }
    }

    [Fact]
    public async Task DeleteDepartment_AsEmployee_ReturnsForbidden()
    {
        var token = await CreateUserAndLoginAsync(
            $"employee_{Guid.NewGuid():N}",
            "Employee");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await _client.DeleteAsync(
                "/api/Department/999999");

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;
        }
    }

    [Fact]
    public async Task DeleteDepartment_NotFound_ReturnsNotFound()
    {
        var token = await CreateUserAndLoginAsync(
            $"admin_{Guid.NewGuid():N}",
            "Admin");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await _client.DeleteAsync(
                "/api/Department/999999");

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;
        }
    }

    [Fact]
    public async Task DeleteDepartment_WithEmployees_ReturnsConflict()
    {
        var token = await CreateUserAndLoginAsync(
            $"admin_{Guid.NewGuid():N}",
            "Admin");

        int departmentId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var department = new Department
            {
                Name = $"Test Department {Guid.NewGuid():N}"
            };

            context.Departments.Add(department);
            await context.SaveChangesAsync();

            var employee = new Employee
            {
                EmployeeNo = $"EMP-{Guid.NewGuid():N}",
                Name = "Test Employee",
                Email = $"test-{Guid.NewGuid():N}@example.com",
                DepartmentId = department.Id,
                Position = "Developer",
                JoinDate = new DateTime(2024, 1, 1),
                IsActive = true
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();

            departmentId = department.Id;
        }

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await _client.DeleteAsync(
                $"/api/Department/{departmentId}");

            Assert.Equal(
                HttpStatusCode.Conflict,
                response.StatusCode);
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;
        }
    }

    [Fact]
    public async Task DeleteDepartment_WithoutEmployees_ReturnsNoContent()
    {
        var token = await CreateUserAndLoginAsync(
            $"admin_{Guid.NewGuid():N}",
            "Admin");

        int departmentId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var department = new Department
            {
                Name = $"Test Department {Guid.NewGuid():N}"
            };

            context.Departments.Add(department);
            await context.SaveChangesAsync();

            departmentId = department.Id;
        }

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await _client.DeleteAsync(
                $"/api/Department/{departmentId}");

            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;
        }
    }

    private async Task<string> CreateUserAndLoginAsync(
        string username,
        string role)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var user = new User
            {
                Username = username,
                Email = $"{username}@example.com",
                Role = role,
                IsActive = true
            };

            var passwordHasher = new PasswordHasher<User>();
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

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        return loginResult.Token;
    }
}