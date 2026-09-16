namespace HealthBr.Application.Features.Auth.Commands;

/// <summary>
/// Input of <c>POST /api/v1/auth/logout</c>: the raw refresh token read from
/// the httpOnly cookie by the controller. Absent cookies never reach this
/// command — logout without a cookie is answered by the controller alone.
/// </summary>
public sealed record LogoutCommand(string Token);
