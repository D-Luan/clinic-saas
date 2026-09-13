namespace HealthBr.Application.Features.Auth.Commands;

/// <summary>
/// Refresh input. The raw token comes exclusively from the httpOnly cookie
/// read by the controller (spec 7.1/9/15.5 amendment) — the request body is
/// ignored.
/// </summary>
public sealed record RefreshTokenCommand(string Token);
