using HealthBr.Infrastructure.MultiTenancy;
using HealthBr.Infrastructure.Persistence;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthBr.IntegrationTests.Persistence;

[Collection(DatabaseCollection.Name)]
public abstract class DatabaseTestBase : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private SqlConnection? _connection;
    private SqlTransaction? _transaction;

    protected DatabaseTestBase(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _connection = new SqlConnection(_fixture.ConnectionString);
        await _connection.OpenAsync();
        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        // Spec 3.5: every test runs inside a transaction that is rolled back.
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    protected HealthBrDbContext CreateContext(Action<TenantContext>? configureTenant = null)
    {
        var tenantContext = new TenantContext();
        configureTenant?.Invoke(tenantContext);

        // EnableRetryOnFailure is intentionally omitted: it forbids the
        // user-initiated rollback transaction above. Retry is an Azure SQL
        // transient-fault concern, configured in the Api bootstrap.
        var options = new DbContextOptionsBuilder<HealthBrDbContext>()
            .UseSqlServer(_connection!)
            .Options;

        var context = new HealthBrDbContext(options, tenantContext);
        context.Database.UseTransaction(_transaction);
        return context;
    }
}
