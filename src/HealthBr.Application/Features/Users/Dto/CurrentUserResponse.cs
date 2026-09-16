namespace HealthBr.Application.Features.Users.Dto;

/// <summary>
/// Body of <c>GET /api/v1/me</c> (spec 9): the authenticated caller's own
/// data. Every field is sourced from the database row identified by the
/// token's <c>sub</c> claim — never echoed from the request (spec 15.1).
/// </summary>
public sealed record CurrentUserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    Guid TenantId,
    DateTime CreatedAt);
