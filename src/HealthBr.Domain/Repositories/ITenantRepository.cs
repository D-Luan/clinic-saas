using HealthBr.Domain.Entities;

namespace HealthBr.Domain.Repositories;

public interface ITenantRepository
{
    /// <summary>
    /// Stages a new tenant for creation. The provisioning flow stages the
    /// admin user alongside it and persists both with a single
    /// <see cref="SaveChangesAsync"/> call, which is atomic (spec 5.1).
    /// </summary>
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists every staged aggregate in one transaction: the tenant and its
    /// admin user are written together or not at all (spec 5.1).
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
