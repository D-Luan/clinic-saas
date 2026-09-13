using HealthBr.Application.Common.Security;
using HealthBr.Domain.Entities;

namespace HealthBr.Application.Features.Auth;

/// <summary>
/// Builds the refresh-token aggregate for a user: generates the raw token
/// (returned to the caller, destined for the cookie) and its entity holding
/// only the SHA-256 hash (spec 5.1 — hash persisted in the DB).
/// </summary>
public static class RefreshTokenIssuer
{
    public static (RefreshToken Entity, string RawToken) CreateNew(User user)
    {
        var rawToken = RefreshTokenGenerator.Generate();
        var now = DateTime.UtcNow;

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            UserId = user.Id,
            Token = RefreshTokenGenerator.Hash(rawToken),
            ExpiresAt = now.AddDays(AuthConstants.RefreshTokenDays),
            CreatedAt = now,
            UpdatedAt = now,
        };

        return (entity, rawToken);
    }
}
