using HealthBr.Application.Common.Dto;
using HealthBr.Application.Common.Interfaces;
using HealthBr.Application.Features.Users.Dto;
using HealthBr.Domain.Entities;
using HealthBr.Domain.Repositories;

namespace HealthBr.Application.Features.Users.Queries;

public sealed class ListUsersQueryHandler(
    IUserRepository userRepository,
    ITenantContext tenantContext)
{
    public async Task<PagedResponse<UserResponse>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        // The tenant is whatever the JWT middleware put in the context —
        // isolation comes from the tenant-scoped repository plus the Global
        // Query Filter, never from the request (spec 15.1/15.7).
        var tenantId = tenantContext.TenantId;

        var total = await userRepository.CountActiveInTenantAsync(tenantId, cancellationToken);
        var users = await userRepository.ListActiveInTenantAsync(
            tenantId,
            skip: (query.Page - 1) * query.PageSize,
            take: query.PageSize,
            cancellationToken);

        var totalPages = (int)Math.Ceiling(total / (double)query.PageSize);
        var items = users.Select(ToResponse).ToList();

        return new PagedResponse<UserResponse>(items, query.Page, query.PageSize, total, totalPages);
    }

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
}
