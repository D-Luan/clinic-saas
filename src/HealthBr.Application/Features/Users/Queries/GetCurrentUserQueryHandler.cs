using HealthBr.Application.Common.Exceptions;
using HealthBr.Application.Common.Interfaces;
using HealthBr.Application.Features.Users.Dto;
using HealthBr.Domain.Repositories;

namespace HealthBr.Application.Features.Users.Queries;

public sealed class GetCurrentUserQueryHandler(
    IUserRepository userRepository,
    ITenantContext tenantContext)
{
    public async Task<CurrentUserResponse> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        // Tenant-scoped lookup: the sub claim is only honored inside the
        // tenant of the token itself (spec 15.1/15.7). A token whose user no
        // longer exists (or was soft-deleted) fails indistinguishably from an
        // unknown id.
        var user = await userRepository.FindActiveInTenantAsync(query.UserId, tenantContext.TenantId, cancellationToken)
            ?? throw new EntityNotFoundException("Usuário não encontrado.");

        return new CurrentUserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.TenantId, user.CreatedAt);
    }
}
