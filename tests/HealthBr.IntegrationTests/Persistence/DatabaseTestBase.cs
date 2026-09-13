using HealthBr.Infrastructure.MultiTenancy;
using HealthBr.Infrastructure.Persistence;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthBr.IntegrationTests.Persistence;

[Collection(DatabaseCollection.Name)]
public abstract class DatabaseTestBase : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;

    protected DatabaseTestBase(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    // Exposed so API-level tests can wire the WebApplicationFactory onto the
    // same connection/transaction pair (task 1.3).
    protected SqlConnection Connection { get; private set; } = null!;

    protected SqlTransaction Transaction { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        Connection = new SqlConnection(_fixture.ConnectionString);
        await Connection.OpenAsync();
        Transaction = (SqlTransaction)await Connection.BeginTransactionAsync();
    }

    public virtual async Task DisposeAsync()
    {
        // Spec 3.5: every test runs inside a transaction that is rolled back.
        if (Transaction is not null)
        {
            await Transaction.RollbackAsync();
            await Transaction.DisposeAsync();
        }

        if (Connection is not null)
        {
            await Connection.DisposeAsync();
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
            .UseSqlServer(Connection)
            .Options;

        var context = new HealthBrDbContext(options, tenantContext);
        context.Database.UseTransaction(Transaction);
        return context;
    }
}
