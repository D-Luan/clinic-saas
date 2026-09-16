using HealthBr.Domain.Entities;
using HealthBr.Domain.Repositories;
using HealthBr.Infrastructure.Persistence;

namespace HealthBr.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository(HealthBrDbContext context) : ITenantRepository
{
    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default) =>
        await context.Tenants.AddAsync(tenant, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
