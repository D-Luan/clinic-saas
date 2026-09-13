namespace HealthBr.Application.Features.Auth.Dto;

/// <summary>
/// Body returned by the login and refresh endpoints. The refresh token is
/// deliberately absent: it travels only in the httpOnly cookie
/// (spec 7.1/15.5).
/// </summary>
public sealed record AccessTokenResponse(string AccessToken);
