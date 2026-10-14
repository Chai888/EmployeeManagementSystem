using EmployeeManagement.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EmployeeManagement.Tests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
        $"EmployeeManagementTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "EmployeeManagement.Tests",
                    ["Jwt:Audience"] = "EmployeeManagement.Tests",
                    ["Jwt:Key"] =
                        "TestOnlySecretKey_AtLeast32Characters!"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<
                DbContextOptions<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(
                options =>
                    options
                        .UseInMemoryDatabase(_databaseName)
                        .ConfigureWarnings(warnings =>
                            warnings.Ignore(
                                InMemoryEventId.TransactionIgnoredWarning)));
        });
    }
}