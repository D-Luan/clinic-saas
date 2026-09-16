using HealthBr.Application.Common.Security;
using HealthBr.Domain.Repositories;

using Microsoft.Extensions.Logging;

namespace HealthBr.Application.Features.Auth.Commands;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ILogger<LogoutCommandHandler> logger)
{
    public async Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenGenerator.Hash(command.Token);
        var stored = await refreshTokenRepository.FindActiveByTokenHashAsync(tokenHash, cancellationToken);

        // Idempotent by design (task 2.2 brief): unknown or already-revoked
        // tokens change nothing and still yield 204 — the response must not
        // leak whether a session ever existed (spec 15.1).
        if (stored is null || stored.RevokedAt is not null)
        {
            logger.LogInformation(
                "Logout: no active refresh token ({TokenPreview})",
                AuthConstants.TokenPreview(command.Token));
            return;
        }

        var now = DateTime.UtcNow;
        stored.RevokedAt = now;
        stored.UpdatedAt = now;
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        // Spec 15.6: logout is a security event; origin IP and trace id ride
        // the request log scope set in the Api pipeline.
        logger.LogInformation(
            "Logout succeeded (UserId={UserId}, TenantId={TenantId})",
            stored.UserId,
            stored.TenantId);
    }
}
