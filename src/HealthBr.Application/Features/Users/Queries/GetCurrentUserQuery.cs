namespace HealthBr.Application.Features.Users.Queries;

/// <summary>
/// Input of <c>GET /api/v1/me</c>. <see cref="UserId"/> comes from the
/// token's <c>sub</c> claim — the only identity the backend trusts
/// (spec 15.1).
/// </summary>
public sealed record GetCurrentUserQuery(Guid UserId);
