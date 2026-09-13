using HealthBr.Domain.Entities;

namespace HealthBr.Domain.Repositories;

public interface IRefreshTokenRepository
{
    /// <summary>
    /// Lookup by the SHA-256 hash of the raw refresh token. Runs outside the
    /// <c>TenantId</c> Global Query Filter because the refresh flow
    /// authenticates via cookie before any tenant context exists (spec 9).
    /// Soft-deleted rows are excluded. Revocation and expiry are caller
    /// concerns.
    /// </summary>
    Task<RefreshToken?> FindActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
