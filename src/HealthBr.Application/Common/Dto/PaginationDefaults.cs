namespace HealthBr.Application.Common.Dto;

/// <summary>
/// Pagination bounds fixed by the spec (section 9): <c>page</c> defaults to 1
/// and <c>pageSize</c> to 20, clamped at 100. List queries normalize their
/// inputs through these helpers so every endpoint applies the same rules
/// (task 3.1+ reuses them).
/// </summary>
public static class PaginationDefaults
{
    public const int DefaultPage = 1;

    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public static int NormalizePage(int? page) => page is >= 1 ? page.Value : DefaultPage;

    public static int NormalizePageSize(int? pageSize) =>
        pageSize is >= 1 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;
}
