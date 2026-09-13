using FluentValidation;

using HealthBr.Application.Common.Exceptions;
using HealthBr.Application.Common.Security;
using HealthBr.Application.Features.Auth.Commands;
using HealthBr.Application.Features.Auth.Dto;
using HealthBr.Application.Features.Auth.Validators;

using Microsoft.AspNetCore.Mvc;

namespace HealthBr.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IValidator<LoginRequest> loginValidator,
    LoginCommandHandler loginHandler,
    RefreshTokenCommandHandler refreshHandler) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        // Error shape is intentionally simple until the global error pipeline
        // lands (task 1.4, spec 10.1).
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToArray());
            return BadRequest(new { message = "Dados inválidos.", errors });
        }

        try
        {
            var result = await loginHandler.HandleAsync(new LoginCommand(request.Email, request.Password), cancellationToken);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(new AccessTokenResponse(result.AccessToken));
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new { message = "Credenciais inválidas." });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        // The raw token lives only in the httpOnly cookie — the body is
        // ignored (spec 7.1/9/15.5 amendment).
        if (!Request.Cookies.TryGetValue(AuthConstants.RefreshTokenCookieName, out var rawToken)
            || string.IsNullOrWhiteSpace(rawToken))
        {
            return Unauthorized(new { message = "Sessão expirada." });
        }

        try
        {
            var result = await refreshHandler.HandleAsync(new RefreshTokenCommand(rawToken), cancellationToken);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(new AccessTokenResponse(result.AccessToken));
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new { message = "Sessão expirada." });
        }
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
