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
}
