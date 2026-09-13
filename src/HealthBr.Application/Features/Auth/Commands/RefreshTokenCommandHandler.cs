using HealthBr.Application.Common.Exceptions;
using HealthBr.Application.Common.Interfaces;
using HealthBr.Application.Common.Security;
using HealthBr.Application.Features.Auth.Dto;
using HealthBr.Domain.Repositories;

using Microsoft.Extensions.Logging;

namespace HealthBr.Application.Features.Auth.Commands;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAccessTokenService accessTokenService,
    ILogger<RefreshTokenCommandHandler> logger)
{
    public async Task<AuthResult> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenGenerator.Hash(command.Token);
        var stored = await refreshTokenRepository.FindActiveByTokenHashAsync(tokenHash, cancellationToken);
        if (stored is null)
        {
            logger.LogWarning(
                "Refresh failed: token not recognized ({TokenPreview})",
                AuthConstants.TokenPreview(command.Token));
            throw new InvalidCredentialsException();
        }

        if (stored.RevokedAt is not null)
        {
            logger.LogWarning(
                "Refresh failed: token already revoked ({TokenPreview}, UserId={UserId})",
                AuthConstants.TokenPreview(command.Token),
                stored.UserId);
            throw new InvalidCredentialsException();
        }

        var now = DateTime.UtcNow;
        if (stored.ExpiresAt <= now)
        {
            logger.LogWarning(
                "Refresh failed: token expired ({TokenPreview}, UserId={UserId})",
                AuthConstants.TokenPreview(command.Token),
                stored.UserId);
            throw new InvalidCredentialsException();
        }

        var user = await userRepository.FindActiveByIdAsync(stored.UserId, cancellationToken);
        if (user is null)
        {
            logger.LogWarning(
                "Refresh failed: user not found or inactive (UserId={UserId})", stored.UserId);
            throw new InvalidCredentialsException();
        }

        // Rotation (spec 5.1): revoke the presented token and issue a new pair
        // in a single save, so the swap is atomic.
        stored.RevokedAt = now;
        stored.UpdatedAt = now;

        var accessToken = accessTokenService.CreateAccessToken(user);
        var (entity, rawToken) = RefreshTokenIssuer.CreateNew(user);
        await refreshTokenRepository.AddAsync(entity, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Refresh succeeded (UserId={UserId}, TenantId={TenantId})",
            user.Id,
            user.TenantId);

        return new AuthResult(user.Id, user.TenantId, accessToken, rawToken);
    }
}
