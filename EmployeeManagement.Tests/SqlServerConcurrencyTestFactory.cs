
using EmployeeManagement.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EmployeeManagement.Tests;

public sealed class SqlServerConcurrencyTestFactory
    : WebApplicationFactory<Program>
{
    private const string ExpectedDatabase =
        "EmployeeManagementConcurrencyTest";

    private readonly string _connectionString;

    public SqlServerConcurrencyTestFactory()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "EMPLOYEE_MANAGEMENT_CONCURRENCY_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set the EMPLOYEE_MANAGEMENT_CONCURRENCY_TEST_CONNECTION " +
                "environment variable before running SQL Server tests.");
        }

        var sqlBuilder =
            new SqlConnectionStringBuilder(connectionString);

        if (!string.Equals(
                sqlBuilder.InitialCatalog,
                ExpectedDatabase,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SQL Server concurrency tests may only use " +
                $"the '{ExpectedDatabase}' database.");
        }

        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] =
                        "EmployeeManagement.Tests",
                    ["Jwt:Audience"] =
                        "EmployeeManagement.Tests",
                    ["Jwt:Key"] =
                        "TestOnlySecretKey_AtLeast32Characters!",
                    ["ConnectionStrings:DefaultConnection"] =
                        _connectionString
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<
                DbContextOptions<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(
                options => options.UseSqlServer(
                    _connectionString));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        // Apply existing EF Core migrations.
        // This does not delete the database or its data.
        await context.Database.MigrateAsync();
    }
}
