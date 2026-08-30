using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Run the API using the Testing environment.
        builder.UseEnvironment("Testing");

        // Supply configuration required when the real API starts.
        // The PostgreSQL connection string is only a dummy value here.
        // The actual TmsDbContext is replaced with EF Core InMemory below.
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] =
                    "ThisIsASecretKeyForTestingPurposesOnly123456!",

                ["Jwt:Secret"] =
                    "ThisIsASecretKeyForTestingPurposesOnly123456!",

                ["Jwt:Issuer"] =
                    "TmsTestIssuer",

                ["Jwt:Audience"] =
                    "TmsTestAudience",

                ["Payments:GatewayUrl"] =
                    "https://test-gateway.example.com",

                ["Payments:MaxDepositBirr"] =
                    "10000",

                // Required by Program.cs when registering the PostgreSQL
                // health check. This is NOT used as the test database.
                ["ConnectionStrings:TmsDatabase"] =
                    "Host=localhost;Database=TmsTest;Username=test;Password=test"
            });
        });

        // Replace the production PostgreSQL DbContext with
        // an isolated EF Core InMemory database.
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TmsDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<TmsDbContext>();

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<TmsDbContext>(options =>
            {
                options.UseInMemoryDatabase("TmsTestDb");
                options.UseInternalServiceProvider(inMemoryProvider);
            });
        });
    }
}
