using Shop.DAL.Models;

namespace Shop.BLL.IRepositories;

/// <summary>
/// Repository contract for <see cref="Product"/>, adding the catalog paging
/// and bulk-load queries on top of the generic surface.
/// </summary>
public interface IProductRepo : IGenericRepo<Product>
{
    /// <summary>
    /// Returns a single page of products plus the total matching count. The
    /// total count is captured BEFORE Skip/Take so the storefront can restore
    /// the user's browse position (page + scroll) after back-navigation —
    /// preserved-position pagination. No <c>out</c>/<c>ref</c> parameters are
    /// used because they are illegal on <c>async</c> methods; the count is
    /// returned as part of the tuple instead.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size (number of items per page).</param>
    /// <param name="categoryId">Optional category filter.</param>
    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, int? categoryId);

    /// <summary>
    /// Bulk-loads the products whose ids are in <paramref name="ids"/> (used by
    /// the admin bulk-update endpoint).
    /// </summary>
    /// <param name="ids">The product ids to load.</param>
    Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids);
}
