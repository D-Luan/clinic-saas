using HealthBr.Application.Common.Dto;

namespace HealthBr.Application.Features.Users.Queries;

/// <summary>
/// Input of <c>GET /api/v1/users</c>. Raw query-string values are normalized
/// in the constructor against the spec 9 pagination bounds (page default 1;
/// pageSize default 20, max 100) so handlers and repositories only ever see
/// valid numbers.
/// </summary>
public sealed record ListUsersQuery
{
    public int Page { get; }

    public int PageSize { get; }

    public ListUsersQuery(int? page, int? pageSize)
    {
        Page = PaginationDefaults.NormalizePage(page);
        PageSize = PaginationDefaults.NormalizePageSize(pageSize);
    }
}
