namespace HealthBr.Application.Features.Auth.Dto;

/// <summary>
/// Internal result of the login and refresh use cases. <see cref="RefreshToken"/>
/// carries the raw token destined for the httpOnly cookie — it must never be
/// logged (spec 15.6).
/// </summary>
public sealed record AuthResult(Guid UserId, Guid TenantId, string AccessToken, string RefreshToken);
