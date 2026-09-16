using HealthBr.Domain.Entities;
using HealthBr.Domain.Repositories;
using HealthBr.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HealthBr.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(HealthBrDbContext context) : IUserRepository
{
    public async Task<IReadOnlyList<User>> FindActiveByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Users
            .IgnoreQueryFilters() // pre-authentication lookup, see IUserRepository
            .Where(user => user.Email == email && !user.IsDeleted)
            .ToListAsync(cancellationToken);

    public async Task<User?> FindActiveByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Users
            .IgnoreQueryFilters() // pre-authentication lookup, see IUserRepository
            .SingleOrDefaultAsync(user => user.Id == id && !user.IsDeleted, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await context.Users.AddAsync(user, cancellationToken);

    // The authenticated surface always pairs the id with the caller's tenant
    // id from the JWT (spec 15.1); the explicit TenantId predicate doubles up
    // with the Global Query Filter so the method stays correct even when the
    // request-scoped filter is backed by an unset tenant context.
    public async Task<User?> FindActiveInTenantAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default) =>
        await context.Users
            .SingleOrDefaultAsync(user => user.Id == id && user.TenantId == tenantId && !user.IsDeleted, cancellationToken);

    // E-mail order: unique per tenant among active users (filtered index), so
    // it is a strict total order — deterministic pagination without the ties
    // CreatedAt (datetime2(0), second precision) would produce within a batch.
    public async Task<IReadOnlyList<User>> ListActiveInTenantAsync(Guid tenantId, int skip, int take, CancellationToken cancellationToken = default) =>
        await context.Users
            .Where(user => user.TenantId == tenantId && !user.IsDeleted)
            .OrderBy(user => user.Email)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<int> CountActiveInTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await context.Users.CountAsync(user => user.TenantId == tenantId && !user.IsDeleted, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
