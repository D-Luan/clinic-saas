using HealthBr.Application.Common.Interfaces;
using HealthBr.Infrastructure.Persistence;
using HealthBr.IntegrationTests.Persistence;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace HealthBr.IntegrationTests.Api;

/// <summary>
/// Captures the tenant id held by the request-scoped <see cref="ITenantContext"/>
/// after the pipeline ran, letting tests assert what the JWT middleware
/// populated (spec 5.1/15.7) without a dedicated authenticated endpoint
/// (those arrive with task 2.2).
/// </summary>
public sealed class TenantProbe
{
    public Guid? TenantIdAfterPipeline { get; set; }
}

internal sealed class TenantProbeFilter(TenantProbe probe) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
                await nextMiddleware();
                probe.TenantIdAfterPipeline = tenantContext.TenantId;
            });
            next(app);
        };
}

/// <summary>
/// Boots the real Api pipeline on top of the shared Testcontainers SQL Server
/// connection and per-test rollback transaction. The JWT signing key is a
/// fixed test-only value supplied through in-memory configuration. The
/// environment defaults to "Testing"; task 1.4 tests boot "Development" and
/// "Production" variants to assert the environment-gated Swagger middleware.
/// </summary>
internal sealed class AuthApiFactory(
    SqlConnection connection,
    SqlTransaction transaction,
    ILoggerProvider loggerProvider,
    TenantProbe tenantProbe,
    string environment = "Testing") : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "integration-test-signing-key-0123456789abcdef";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Non-Development environments keep user-secrets out of the test
        // host; a "Development" boot may load them, but the environment
        // variables set by ApiTestBase (connection string + JWT signing
        // key) win over user-secrets in the configuration order and satisfy
        // the Program.cs startup guards either way.
        builder.UseEnvironment(environment);
        builder.ConfigureTestServices(services =>
        {
            // Replace the app DbContext with one bound to the shared
            // connection + rollback transaction. Retry is dropped for the
            // same reason as in DatabaseTestBase.
            services.RemoveAll<HealthBrDbContext>();
            services.RemoveAll<DbContextOptions<HealthBrDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<HealthBrDbContext>));

            var options = new DbContextOptionsBuilder<HealthBrDbContext>()
                .UseSqlServer(connection)
                .Options;
            services.AddSingleton(options);
            services.AddScoped<HealthBrDbContext>(sp =>
            {
                var context = new HealthBrDbContext(
                    sp.GetRequiredService<DbContextOptions<HealthBrDbContext>>(),
                    sp.GetRequiredService<ITenantContext>());
                context.Database.UseTransaction(transaction);
                return context;
            });

            services.AddSingleton(loggerProvider);
            services.AddSingleton(tenantProbe);
            services.AddSingleton<IStartupFilter, TenantProbeFilter>();
        });
    }
}
