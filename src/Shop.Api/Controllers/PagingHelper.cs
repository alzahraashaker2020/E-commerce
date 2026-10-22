namespace Shop.Api.Controllers;

/// <summary>
/// Shared paging-parameter clamp helper so every paged endpoint (catalog and
/// the admin orders/customers lists) applies the same rules: page is at least
/// 1, and pageSize is held within 1-100 with a default of 12.
/// </summary>
public static class PagingHelper
{
    /// <summary>Default page size used when the request supplies none.</summary>
    public const int DefaultPageSize = 12;

    /// <summary>Maximum page size a caller may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Clamps the supplied paging parameters to valid bounds.
    /// </summary>
    /// <param name="page">Requested page number.</param>
    /// <param name="pageSize">Requested page size.</param>
    /// <returns>The clamped page and page size.</returns>
    public static (int Page, int PageSize) Clamp(int page, int pageSize)
    {
        int clampedPage = page < 1 ? 1 : page;

        int clampedPageSize = pageSize < 1
            ? DefaultPageSize
            : (pageSize > MaxPageSize ? MaxPageSize : pageSize);

        return (clampedPage, clampedPageSize);
    }
}
