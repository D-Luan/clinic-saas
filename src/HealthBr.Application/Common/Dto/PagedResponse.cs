namespace HealthBr.Application.Common.Dto;

/// <summary>
/// The standard list contract of the API (spec 9 / 7.5): every paged
/// endpoint answers with <c>{ items, page, pageSize, total, totalPages }</c>.
/// It is generic on purpose — task 3.1 (patients) and later list endpoints
/// reuse this envelope instead of redefining it.
/// </summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);
