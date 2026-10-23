using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.DAL.Data;
using Shop.DAL.Models;

namespace Shop.BLL.Repositories;

/// <summary>
/// EF Core repository for <see cref="Product"/>, implementing the catalog
/// paging and bulk-load queries.
/// </summary>
public class ProductRepo : GenericRepo<Product>, IProductRepo
{
    /// <summary>
    /// Creates a new <see cref="ProductRepo"/>.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public ProductRepo(ShopContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, int? categoryId)
    {
        // Build a deferred query and apply the optional category filter.
        IQueryable<Product> query = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Variants);

        if (categoryId.HasValue)
        {
            // Match products on the category itself OR on any of its
            // subcategories. Products are attached to leaf (sub)categories, so
            // filtering by a top-level category ("Sofa Styling") must include
            // everything under its children too; filtering by a leaf category
            // matches only that leaf.
            query = query.Where(p =>
                p.CategoryId == categoryId.Value ||
                p.Category!.ParentCategoryId == categoryId.Value);
        }

        // Capture the total count BEFORE Skip/Take. Returning TotalCount is what
        // enables preserved-position pagination: the storefront uses it to
        // restore the exact page (and scroll) the user left when they go back.
        int totalCount = await query.CountAsync();

        List<Product> items = await query
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids)
    {
        return _context.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
    }
}
