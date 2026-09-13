using HealthBr.Application.Common.Interfaces;

namespace HealthBr.Infrastructure.MultiTenancy;

/// <summary>
/// Provisional <see cref="ITenantContext"/> implementation for the persistence
/// layer (task 1.2). Task 1.3 replaces the population mechanism with the JWT
/// authentication middleware, the only intended caller of
/// <see cref="SetTenantId"/> (spec 15.7).
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }

    public void SetTenantId(Guid tenantId) => TenantId = tenantId;
}
