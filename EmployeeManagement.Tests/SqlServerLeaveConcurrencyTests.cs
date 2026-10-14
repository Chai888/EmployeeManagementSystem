
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EmployeeManagement.Tests;

public class SqlServerLeaveConcurrencyTests
    : IClassFixture<SqlServerConcurrencyTestFactory>,
      IAsyncLifetime
{
    private readonly SqlServerConcurrencyTestFactory _factory;

    public SqlServerLeaveConcurrencyTests(
        SqlServerConcurrencyTestFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    private async Task<(int EmployeeId, string Token)>
        CreateEmployeeAsync(decimal entitledDays = 14m)
    {
        var username = $"sqltest_{Guid.NewGuid():N}";

        int employeeId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var department = new Department
            {
                Name = $"SQLTestDept_{Guid.NewGuid():N}"
            };

            context.Departments.Add(department);
            await context.SaveChangesAsync();

            var employee = new Employee
            {
                EmployeeNo = $"SQL-{Guid.NewGuid():N}",
                Name = "SQL Concurrency Test Employee",
                Email = $"{username}@example.com",
                DepartmentId = department.Id,
                Position = "Developer",
                JoinDate = new DateTime(2024, 1, 1),
                IsActive = true
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();

            employeeId = employee.Id;

            var user = new User
            {
                Username = username,
                Email = employee.Email,
                Role = "Employee",
                IsActive = true,
                EmployeeId = employee.Id
            };

            user.PasswordHash =
                new PasswordHasher<User>().HashPassword(
                    user,
                    "TestPassword123!");

            context.Users.Add(user);

            context.LeaveBalances.Add(new LeaveBalance
            {
                EmployeeId = employee.Id,
                Year = 2026,
                LeaveType = "Annual Leave",
                EntitledDays = entitledDays,
                UsedDays = 0m
            });

            await context.SaveChangesAsync();
        }

        using var client = CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
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
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(
            string.IsNullOrWhiteSpace(loginResult.Token));

        return (employeeId, loginResult.Token);
    }

    private async Task<string> CreateAdminAndLoginAsync()
    {
        var username = $"sqladmin_{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var admin = new User
            {
                Username = username,
                Email = $"{username}@example.com",
                Role = "Admin",
                IsActive = true
            };

            admin.PasswordHash =
                new PasswordHasher<User>().HashPassword(
                    admin,
                    "TestPassword123!");

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }

        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                username,
                password = "TestPassword123!"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        return result.Token;
    }

    private async Task<LeaveRequestResponse> SubmitLeaveAsync(
        string employeeToken,
        DateTime startDate,
        DateTime endDate)
    {
        using var client = CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                employeeToken);

        var response = await client.PostAsJsonAsync(
            "/api/LeaveRequest",
            new
            {
                leaveType = "Annual Leave",
                startDate,
                endDate,
                reason = "SQL Server concurrency test"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<LeaveRequestResponse>();

        Assert.NotNull(result);

        return result;
    }

    [Fact]
    public async Task ConcurrentApproval_SameRequest_DeductsBalanceOnlyOnce()
    {
        // Arrange: create one employee and one pending leave request.
        var employee = await CreateEmployeeAsync(14m);

        var leaveRequest = await SubmitLeaveAsync(
            employee.Token,
            new DateTime(2026, 11, 16),
            new DateTime(2026, 11, 17));

        var adminToken = await CreateAdminAndLoginAsync();

        // Use separate clients so each request has independent headers.
        using var client1 = CreateClient();
        using var client2 = CreateClient();

        client1.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        client2.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        var url =
            $"/api/LeaveRequest/{leaveRequest.Id}/status";

        var payload = new { status = "Approved" };

        // Act: submit both approvals concurrently.
        var responses = await Task.WhenAll(
            client1.PutAsJsonAsync(url, payload),
            client2.PutAsJsonAsync(url, payload));

        // Exactly one approval should succeed.
        Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.OK);

        // Verify the final persisted state, not just HTTP responses.
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var savedRequest = await context.LeaveRequests
            .AsNoTracking()
            .SingleAsync(x => x.Id == leaveRequest.Id);

        var balance = await context.LeaveBalances
            .AsNoTracking()
            .SingleAsync(x =>
                x.EmployeeId == employee.EmployeeId &&
                x.Year == 2026 &&
                x.LeaveType == "Annual Leave");

        Assert.Equal("Approved", savedRequest.Status);
        Assert.Equal(2m, balance.UsedDays);
        Assert.Equal(12m, balance.RemainingDays);
    }

    [Fact]
    public async Task ConcurrentOverlappingRequests_OnlyOneIsCreated()
    {
        // Arrange: one employee submits two overlapping requests.
        var employee = await CreateEmployeeAsync(14m);

        using var client1 = CreateClient();
        using var client2 = CreateClient();

        client1.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                employee.Token);

        client2.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                employee.Token);

        var request1 = new
        {
            leaveType = "Annual Leave",
            startDate = new DateTime(2026, 11, 16),
            endDate = new DateTime(2026, 11, 18),
            reason = "Concurrent overlap test A"
        };

        var request2 = new
        {
            leaveType = "Annual Leave",
            startDate = new DateTime(2026, 11, 18),
            endDate = new DateTime(2026, 11, 20),
            reason = "Concurrent overlap test B"
        };

        // Act: both requests start without waiting for each other.
        var responses = await Task.WhenAll(
            client1.PostAsJsonAsync(
                "/api/LeaveRequest",
                request1),
            client2.PostAsJsonAsync(
                "/api/LeaveRequest",
                request2));

        // Exactly one request should be created.
        Assert.Single(
            responses,
            response =>
                response.StatusCode == HttpStatusCode.Created);

        // Check how many requests were actually persisted.
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var savedRequests = await context.LeaveRequests
            .AsNoTracking()
            .Where(x => x.EmployeeId == employee.EmployeeId)
            .ToListAsync();

        Assert.Single(savedRequests);

        Assert.Equal(
            "Pending",
            savedRequests[0].Status);
    }
}
