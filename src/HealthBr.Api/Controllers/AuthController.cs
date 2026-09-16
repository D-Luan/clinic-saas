using HealthBr.Application.Common.Exceptions;
using HealthBr.Application.Common.Security;
using HealthBr.Application.Features.Auth.Commands;
using HealthBr.Application.Features.Auth.Dto;

using Microsoft.AspNetCore.Mvc;

namespace HealthBr.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    LoginCommandHandler loginHandler,
    RefreshTokenCommandHandler refreshHandler) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        // Validation happens in FluentValidationFilter and failures surface
        // as ProblemDetails through the global error pipeline (spec 10.1).
        var result = await loginHandler.HandleAsync(new LoginCommand(request.Email, request.Password), cancellationToken);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new AccessTokenResponse(result.AccessToken));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        // The raw token lives only in the httpOnly cookie — the body is
        // ignored (spec 7.1/9/15.5 amendment).
        if (!Request.Cookies.TryGetValue(AuthConstants.RefreshTokenCookieName, out var rawToken)
            || string.IsNullOrWhiteSpace(rawToken))
        {
            // Generic 401 through the global pipeline: never reveals whether
            // a session ever existed (spec 15.1).
            throw new InvalidCredentialsException("Sessão expirada.");
        }

        var result = await refreshHandler.HandleAsync(new RefreshTokenCommand(rawToken), cancellationToken);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new AccessTokenResponse(result.AccessToken));
    }

    // Spec 15.5: HttpOnly, Secure, SameSite=Strict, Path=/api/v1/auth, 7 days.
    private void SetRefreshTokenCookie(string rawToken)
    {
        Response.Cookies.Append(
            AuthConstants.RefreshTokenCookieName,
            rawToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = AuthConstants.RefreshTokenCookiePath,
                MaxAge = TimeSpan.FromDays(AuthConstants.RefreshTokenDays),
                Expires = DateTimeOffset.UtcNow.AddDays(AuthConstants.RefreshTokenDays),
            });
    }
}
