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

    /// <summary>
    /// Stages a new user; it is persisted when the caller saves. Tenant
    /// provisioning stages tenant and admin user together and writes both
    /// with a single save (spec 5.1).
    /// </summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenant-scoped lookup by id for the authenticated surface (spec 15.1):
    /// identity data comes from the JWT, and the user is only found when it
    /// belongs to the given tenant and is active — a token pointing at
    /// another tenant's user matches nothing (fail-safe, spec 15.7).
    /// </summary>
    Task<User?> FindActiveInTenantAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// One page of the tenant's active users in e-mail order (spec 9:
    /// deterministic pagination — e-mail is unique per tenant among active
    /// users, a strict total order). <paramref name="skip"/>/<paramref name="take"/>
    /// are caller-computed so the repository stays free of paging conventions.
    /// </summary>
    Task<IReadOnlyList<User>> ListActiveInTenantAsync(Guid tenantId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Total active users in the tenant, for the <c>total</c>/<c>totalPages</c>
    /// members of the paged list contract (spec 9).
    /// </summary>
    Task<int> CountActiveInTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists staged users in one implicit transaction (single save, same
    /// contract as <see cref="ITenantRepository.SaveChangesAsync"/>).
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
