using HealthBr.Infrastructure.MultiTenancy;
using HealthBr.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

namespace HealthBr.IntegrationTests.Persistence;

public sealed class SqlServerFixture : IAsyncLifetime
{
    // Same image as the local dev container (spec 12.4).
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<HealthBrDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var context = new HealthBrDbContext(options, new TenantContext());
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "Database";
}
