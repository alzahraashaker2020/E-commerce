namespace Shop.DAL.ViewModels;

/// <summary>
/// Generic paged result carrying the page's items plus the total count and
/// paging metadata, so the client can restore page and scroll position.
/// </summary>
/// <typeparam name="T">Type of the items in the page.</typeparam>
public class PagedResultVM<T>
{
    /// <summary>Items on the current page.</summary>
    public List<T> Items { get; set; } = new List<T>();

    /// <summary>Total number of items across all pages.</summary>
    public int TotalCount { get; set; }

    /// <summary>1-based current page number.</summary>
    public int Page { get; set; }

    /// <summary>Number of items per page.</summary>
    public int PageSize { get; set; }
}
