using HealthBr.Infrastructure.MultiTenancy;
using HealthBr.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HealthBr.IntegrationTests.Persistence;

[Collection(DatabaseCollection.Name)]
public sealed class MigrationTests
{
    private readonly SqlServerFixture _fixture;

    public MigrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migrate_ShouldApplyInitialCreate()
    {
        var options = new DbContextOptionsBuilder<HealthBrDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;

        await using var context = new HealthBrDbContext(options, new TenantContext());
        var applied = await context.Database.GetAppliedMigrationsAsync();

        Assert.Contains(applied, migration => migration.EndsWith("InitialCreate", StringComparison.Ordinal));
    }
}
