namespace HealthBr.Application.Features.Tenants.Dto;

/// <summary>
/// 201 body of <c>POST /api/v1/tenants</c>. Deliberately minimal — just the
/// tenant id: no tokens and no automatic login. After provisioning, the
/// client authenticates via <c>POST /api/v1/auth/login</c> (spec 2/5.1).
/// </summary>
public sealed record CreateTenantResponse(Guid TenantId);
