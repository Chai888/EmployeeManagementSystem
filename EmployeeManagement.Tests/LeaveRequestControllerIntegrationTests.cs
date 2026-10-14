using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace EmployeeManagement.Tests;

public class LeaveRequestControllerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public LeaveRequestControllerIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    private async Task<string> CreateUserAndLoginAsync(string role)
    {
        var username = $"{role.ToLower()}_{Guid.NewGuid():N}";

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
                EmployeeId = null
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

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        return loginResult.Token;
    }


    private async Task<string> CreateEmployeeWithLeaveBalanceAndLoginAsync(
        decimal entitledDays = 14m)
        {
            var username = $"employee_{Guid.NewGuid():N}";

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var department = new Department
                {
                    Name = $"Department_{Guid.NewGuid():N}"
                };

                context.Departments.Add(department);
                await context.SaveChangesAsync();

                var employee = new Employee
                {
                    EmployeeNo = $"EMP-{Guid.NewGuid():N}",
                    Name = "Leave Test Employee",
                    Email = $"{username}@example.com",
                    DepartmentId = department.Id,
                    Position = "Developer",
                    JoinDate = new DateTime(2024, 1, 1),
                    IsActive = true
                };

                context.Employees.Add(employee);
                await context.SaveChangesAsync();

                var user = new User
                {
                    Username = username,
                    Email = $"{username}@example.com",
                    Role = "Employee",
                    IsActive = true,
                    EmployeeId = employee.Id
                };

                user.PasswordHash = new PasswordHasher<User>()
                    .HashPassword(user, "TestPassword123!");

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

    [Fact]
    public async Task Create_EndDateBeforeStartDate_ReturnsBadRequest()
    {
        var token = await CreateEmployeeWithLeaveBalanceAndLoginAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            "/api/LeaveRequest",
            new
            {
                leaveType = "Annual Leave",
                startDate = new DateTime(2026, 11, 10),
                endDate = new DateTime(2026, 11, 9),
                reason = "Invalid date range"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Create_CrossCalendarYear_ReturnsBadRequest()
    {
        var token = await CreateEmployeeWithLeaveBalanceAndLoginAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            "/api/LeaveRequest",
            new
            {
                leaveType = "Annual Leave",
                startDate = new DateTime(2026, 12, 30),
                endDate = new DateTime(2027, 1, 2),
                reason = "Cross-year leave"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Create_InsufficientLeaveBalance_ReturnsBadRequest()
        {
            // Arrange: employee has only 2 days of annual leave.
            var token = await CreateEmployeeWithLeaveBalanceAndLoginAsync(
                entitledDays: 2m);

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act: request 5 working days.
            var response = await _client.PostAsJsonAsync(
                "/api/LeaveRequest",
                new
                {
                    leaveType = "Annual Leave",
                    startDate = new DateTime(2026, 11, 2), // Monday
                    endDate = new DateTime(2026, 11, 6),   // Friday
                    reason = "Insufficient balance test"
                });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.Contains("Insufficient", responseBody);

            _client.DefaultRequestHeaders.Authorization = null;
        }

    [Fact]
    public async Task Create_OverlappingLeaveRequest_ReturnsBadRequest()
    {
        // Arrange
        var token = await CreateEmployeeWithLeaveBalanceAndLoginAsync(
            entitledDays: 14m);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var firstRequest = await _client.PostAsJsonAsync(
            "/api/LeaveRequest",
            new
            {
                leaveType = "Annual Leave",
                startDate = new DateTime(2026, 11, 2),
                endDate = new DateTime(2026, 11, 4),
                reason = "First leave request"
            });

        Assert.Equal(HttpStatusCode.Created, firstRequest.StatusCode);

        // Act: the second request overlaps with the first request.
        var secondRequest = await _client.PostAsJsonAsync(
            "/api/LeaveRequest",
            new
            {
                leaveType = "Annual Leave",
                startDate = new DateTime(2026, 11, 4),
                endDate = new DateTime(2026, 11, 6),
                reason = "Overlapping leave request"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, secondRequest.StatusCode);

        var responseBody = await secondRequest.Content.ReadAsStringAsync();
        Assert.Contains("overlaps", responseBody);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task UpdateStatus_ApproveLeaveRequest_UpdatesLeaveBalance()
    {
        // Arrange: create an employee with 14 days of annual leave.
        var employeeToken =
            await CreateEmployeeWithLeaveBalanceAndLoginAsync(
                entitledDays: 14m);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken);

        // Employee submits a leave request for Monday and Tuesday.
        var createResponse = await _client.PostAsJsonAsync(
            "/api/LeaveRequest",
            new
            {
                leaveType = "Annual Leave",
                startDate = new DateTime(2026, 11, 2),
                endDate = new DateTime(2026, 11, 3),
                reason = "Approval integration test"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var leaveRequest =
            await createResponse.Content.ReadFromJsonAsync<LeaveRequestResponse>();

        Assert.NotNull(leaveRequest);
        Assert.Equal("Pending", leaveRequest.Status);

        // Create and log in as an administrator.
        var adminToken = await CreateUserAndLoginAsync("Admin");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        // Act: administrator approves the request.
        var approveResponse = await _client.PutAsJsonAsync(
            $"/api/LeaveRequest/{leaveRequest.Id}/status",
            new
            {
                status = "Approved"
            });

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var approvedRequest =
            await approveResponse.Content.ReadFromJsonAsync<LeaveRequestResponse>();

        Assert.NotNull(approvedRequest);
        Assert.Equal("Approved", approvedRequest.Status);
        Assert.NotNull(approvedRequest.ApprovedBy);
        Assert.NotNull(approvedRequest.ApprovedDate);

        // Assert: the approved leave days were persisted to the database.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var balance = await context.LeaveBalances.FirstOrDefaultAsync(x =>
                x.EmployeeId == leaveRequest.EmployeeId &&
                x.Year == 2026 &&
                x.LeaveType == "Annual Leave");

            Assert.NotNull(balance);
            Assert.Equal(2m, balance.UsedDays);
            Assert.Equal(12m, balance.RemainingDays);
        }

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task UpdateStatus_RejectLeaveRequest_DoesNotDeductLeaveBalance()
    {
        // Arrange: create an employee with 14 days of annual leave.
        var employeeToken =
            await CreateEmployeeWithLeaveBalanceAndLoginAsync(
                entitledDays: 14m);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/LeaveRequest",
            new
            {
                leaveType = "Annual Leave",
                startDate = new DateTime(2026, 11, 2),
                endDate = new DateTime(2026, 11, 3),
                reason = "Rejection integration test"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var leaveRequest =
            await createResponse.Content.ReadFromJsonAsync<LeaveRequestResponse>();

        Assert.NotNull(leaveRequest);

        var adminToken = await CreateUserAndLoginAsync("Admin");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        // Act: administrator rejects the request.
        var rejectResponse = await _client.PutAsJsonAsync(
            $"/api/LeaveRequest/{leaveRequest.Id}/status",
            new
            {
                status = "Rejected"
            });

        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);

        var rejectedRequest =
            await rejectResponse.Content.ReadFromJsonAsync<LeaveRequestResponse>();

        Assert.NotNull(rejectedRequest);
        Assert.Equal("Rejected", rejectedRequest.Status);
        Assert.NotNull(rejectedRequest.ApprovedBy);
        Assert.NotNull(rejectedRequest.ApprovedDate);

        // Assert: rejecting the request must not consume leave balance.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var balance = await context.LeaveBalances.FirstOrDefaultAsync(x =>
                x.EmployeeId == leaveRequest.EmployeeId &&
                x.Year == 2026 &&
                x.LeaveType == "Annual Leave");

            Assert.NotNull(balance);
            Assert.Equal(0m, balance.UsedDays);
            Assert.Equal(14m, balance.RemainingDays);
        }

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task UpdateStatus_AlreadyApprovedRequest_ReturnsBadRequest()
    {
        // Arrange: create an employee and submit a leave request.
        var employeeToken =
            await CreateEmployeeWithLeaveBalanceAndLoginAsync(
                entitledDays: 14m);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/LeaveRequest",
            new
            {
                leaveType = "Annual Leave",
                startDate = new DateTime(2026, 11, 2),
                endDate = new DateTime(2026, 11, 3),
                reason = "Duplicate approval test"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var leaveRequest =
            await createResponse.Content.ReadFromJsonAsync<LeaveRequestResponse>();

        Assert.NotNull(leaveRequest);

        var adminToken = await CreateUserAndLoginAsync("Admin");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        // First approval should succeed.
        var firstResponse = await _client.PutAsJsonAsync(
            $"/api/LeaveRequest/{leaveRequest.Id}/status",
            new { status = "Approved" });

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act: try to approve the same request again.
        var secondResponse = await _client.PutAsJsonAsync(
            $"/api/LeaveRequest/{leaveRequest.Id}/status",
            new { status = "Approved" });

        // Assert: repeated approval must be rejected.
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        // Ensure the balance was deducted only once.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var balance = await context.LeaveBalances.FirstOrDefaultAsync(x =>
                x.EmployeeId == leaveRequest.EmployeeId &&
                x.Year == 2026 &&
                x.LeaveType == "Annual Leave");

            Assert.NotNull(balance);
            Assert.Equal(2m, balance.UsedDays);
            Assert.Equal(12m, balance.RemainingDays);
        }

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/LeaveRequest");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_AsEmployee_ReturnsForbidden()
    {
        var token = await CreateUserAndLoginAsync("Employee");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/LeaveRequest");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_AsAdmin_ReturnsForbidden()
    {
        var token = await CreateUserAndLoginAsync("Admin");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            "/api/LeaveRequest",
            new
            {
                leaveType = "Annual Leave",
                startDate = new DateTime(2026, 11, 2),
                endDate = new DateTime(2026, 11, 3),
                reason = "Integration test"
            });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_AsEmployee_ReturnsForbidden()
    {
        var token = await CreateUserAndLoginAsync("Employee");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync(
            "/api/LeaveRequest/1/status",
            new
            {
                status = "Approved"
            });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task WorkingDays_WeekdaysOnly_ReturnsFiveDays()
    {
        // Arrange
        var token = await CreateEmployeeWithLeaveBalanceAndLoginAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act: Monday to Friday, with no public holidays.
        var response = await _client.GetAsync(
            "/api/LeaveRequest/working-days" +
            "?startDate=2026-11-02&endDate=2026-11-06");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var workingDays =
            await response.Content.ReadFromJsonAsync<decimal>();

        Assert.Equal(5m, workingDays);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task WorkingDays_WeekendIsExcluded_ReturnsOneDay()
    {
        // Arrange
        var token = await CreateEmployeeWithLeaveBalanceAndLoginAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act: Saturday to Monday.
        var response = await _client.GetAsync(
            "/api/LeaveRequest/working-days" +
            "?startDate=2026-10-31&endDate=2026-11-02");

        // Assert: only Monday counts.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var workingDays =
            await response.Content.ReadFromJsonAsync<decimal>();

        Assert.Equal(1m, workingDays);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task WorkingDays_PublicHolidayIsExcluded_ReturnsTwoDays()
    {
        var holidayName = $"Test Holiday {Guid.NewGuid():N}";

        try
        {
            // Arrange: add a test holiday on Tuesday.
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                context.PublicHolidays.Add(new PublicHoliday
                {
                    Name = holidayName,
                    Date = new DateTime(2026, 11, 3),
                    Description = "Integration test"
                });

                await context.SaveChangesAsync();
            }

            var token = await CreateEmployeeWithLeaveBalanceAndLoginAsync();

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync(
                "/api/LeaveRequest/working-days" +
                "?startDate=2026-11-02&endDate=2026-11-04");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var workingDays =
                await response.Content.ReadFromJsonAsync<decimal>();

            Assert.Equal(2m, workingDays);
        }
        finally
        {
            // Always remove the test holiday, even if an assertion fails.
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var holiday = await context.PublicHolidays
                    .FirstOrDefaultAsync(x => x.Name == holidayName);

                if (holiday != null)
                {
                    context.PublicHolidays.Remove(holiday);
                    await context.SaveChangesAsync();
                }
            }

            _client.DefaultRequestHeaders.Authorization = null;
        }
    }

    [Fact]
    public async Task WorkingDays_EndDateBeforeStartDate_ReturnsBadRequest()
    {
        // Arrange
        var token = await CreateEmployeeWithLeaveBalanceAndLoginAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(
            "/api/LeaveRequest/working-days" +
            "?startDate=2026-11-10&endDate=2026-11-09");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task WorkingDays_CrossCalendarYear_ReturnsBadRequest()
    {
        // Arrange
        var token = await CreateEmployeeWithLeaveBalanceAndLoginAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(
            "/api/LeaveRequest/working-days" +
            "?startDate=2026-12-30&endDate=2027-01-02");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task UpdateStatus_InsufficientBalanceAtApproval_ReturnsBadRequest()
    {
        // Arrange: employee initially has 2 days of annual leave.
        var employeeToken =
            await CreateEmployeeWithLeaveBalanceAndLoginAsync(
                entitledDays: 2m);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeToken);

        // Submit a request for two weekdays.
        var createResponse = await _client.PostAsJsonAsync(
            "/api/LeaveRequest",
            new
            {
                leaveType = "Annual Leave",
                startDate = new DateTime(2026, 11, 9),
                endDate = new DateTime(2026, 11, 10),
                reason = "Approval balance validation test"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var leaveRequest =
            await createResponse.Content.ReadFromJsonAsync<LeaveRequestResponse>();

        Assert.NotNull(leaveRequest);

        // Simulate the employee's available balance decreasing
        // before the administrator approves the request.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var balance = await context.LeaveBalances.FirstOrDefaultAsync(x =>
                x.EmployeeId == leaveRequest.EmployeeId &&
                x.Year == 2026 &&
                x.LeaveType == "Annual Leave");

            Assert.NotNull(balance);

            balance.UsedDays = 1m;
            await context.SaveChangesAsync();
        }

        // Log in as administrator.
        var adminToken = await CreateUserAndLoginAsync("Admin");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        // Act: approval should fail because only 1 day remains.
        var approveResponse = await _client.PutAsJsonAsync(
            $"/api/LeaveRequest/{leaveRequest.Id}/status",
            new
            {
                status = "Approved"
            });

        // Assert: approval is rejected.
        Assert.Equal(
            HttpStatusCode.BadRequest,
            approveResponse.StatusCode);

        // Verify neither the request status nor balance was incorrectly changed.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var savedRequest = await context.LeaveRequests
                .FirstOrDefaultAsync(x => x.Id == leaveRequest.Id);

            var balance = await context.LeaveBalances.FirstOrDefaultAsync(x =>
                x.EmployeeId == leaveRequest.EmployeeId &&
                x.Year == 2026 &&
                x.LeaveType == "Annual Leave");

            Assert.NotNull(savedRequest);
            Assert.Equal("Pending", savedRequest.Status);

            Assert.NotNull(balance);
            Assert.Equal(1m, balance.UsedDays);
            Assert.Equal(1m, balance.RemainingDays);
        }

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Create_WeekendOnlyDates_ReturnsBadRequest()
    {
        // Arrange
        var token = await CreateEmployeeWithLeaveBalanceAndLoginAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // November 7–8, 2026 are Saturday and Sunday.
        var request = new
        {
            leaveType = "Annual Leave",
            startDate = new DateTime(2026, 11, 7),
            endDate = new DateTime(2026, 11, 8),
            reason = "Weekend-only leave test"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/LeaveRequest",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }
}