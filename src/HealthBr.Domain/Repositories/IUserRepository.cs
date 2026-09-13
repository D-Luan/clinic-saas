using HealthBr.Domain.Entities;

namespace HealthBr.Domain.Repositories;

public interface IUserRepository
{
    /// <summary>
    /// Cross-tenant, pre-authentication lookup by e-mail (login flow). It runs
    /// outside the <c>TenantId</c> Global Query Filter on purpose: there is no
    /// tenant context before the JWT is issued (spec 5.1 amendment). E-mails
    /// are expected to be globally unique in practice; when more than one
    /// active user matches, callers must treat the credentials as invalid.
    /// </summary>
    Task<IReadOnlyList<User>> FindActiveByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cross-tenant lookup by id used by the refresh flow, which also runs
    /// before any tenant context exists. Soft-deleted users are excluded.
    /// </summary>
    Task<User?> FindActiveByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
