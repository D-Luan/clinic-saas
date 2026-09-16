namespace HealthBr.Application.Features.Tenants.Dto;

/// <summary>
/// Use case result of the provisioning flow: the id of the tenant that was
/// created together with its first admin user.
/// </summary>
public sealed record CreateTenantResult(Guid TenantId);
