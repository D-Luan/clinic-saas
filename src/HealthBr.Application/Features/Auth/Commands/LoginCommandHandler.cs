using HealthBr.Application.Common.Exceptions;
using HealthBr.Application.Common.Interfaces;
using HealthBr.Application.Common.Security;
using HealthBr.Application.Features.Auth.Dto;
using HealthBr.Domain.Repositories;

using Microsoft.Extensions.Logging;

namespace HealthBr.Application.Features.Auth.Commands;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokenService,
    ILogger<LoginCommandHandler> logger)
{
    public async Task<AuthResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        // Pre-authentication lookup: crosses the tenant Global Query Filter by
        // design (spec 5.1 amendment); ambiguity is indistinguishable from
        // unknown e-mail in the response (spec 15.1) but logged (spec 15.6).
        var candidates = await userRepository.FindActiveByEmailAsync(command.Email, cancellationToken);
        if (candidates.Count != 1)
        {
            logger.LogWarning(
                "Login failed for {Email}: {Reason}",
                command.Email,
                candidates.Count == 0 ? "no active user with this e-mail" : "ambiguous e-mail across tenants");
            throw new InvalidCredentialsException();
        }

        var user = candidates[0];
        if (!passwordHasher.VerifyHashedPassword(user.PasswordHash, command.Password))
        {
            logger.LogWarning("Login failed for {Email}: {Reason}", command.Email, "invalid password");
            throw new InvalidCredentialsException();
        }

        var accessToken = accessTokenService.CreateAccessToken(user);
        var (entity, rawToken) = RefreshTokenIssuer.CreateNew(user);
        await refreshTokenRepository.AddAsync(entity, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Login succeeded for {Email} (UserId={UserId}, TenantId={TenantId})",
            user.Email,
            user.Id,
            user.TenantId);

        return new AuthResult(user.Id, user.TenantId, accessToken, rawToken);
    }
}
