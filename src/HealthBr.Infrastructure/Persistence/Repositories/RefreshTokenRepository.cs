using HealthBr.Domain.Entities;
using HealthBr.Domain.Repositories;
using HealthBr.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HealthBr.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(HealthBrDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> FindActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens
            .IgnoreQueryFilters() // cookie auth runs before any tenant context exists (spec 9)
            .SingleOrDefaultAsync(token => token.Token == tokenHash && !token.IsDeleted, cancellationToken);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.AddAsync(refreshToken, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
