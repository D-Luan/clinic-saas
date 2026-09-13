namespace HealthBr.Application.Common.Interfaces;

/// <summary>
/// Holds the tenant id for the current request scope. In production it is
/// populated from the <c>tenant_id</c> claim of the JWT by the authentication
/// middleware (spec 5.1/15.7). An unset context keeps <see cref="TenantId"/>
/// as <see cref="Guid.Empty"/>, which the Global Query Filter matches against
/// nothing (fail-safe).
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
}
