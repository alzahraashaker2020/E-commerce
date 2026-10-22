using Microsoft.EntityFrameworkCore;
using Shop.BLL.IRepositories;
using Shop.DAL.Data;

namespace Shop.BLL.Repositories;

/// <summary>
/// Generic EF Core repository base implementing the shared CRUD surface against
/// <c>ShopContext.Set&lt;T&gt;()</c>. Per-entity repos derive from this type to
/// reuse the common operations and add entity-specific queries.
/// </summary>
/// <typeparam name="T">The entity type managed by the repository.</typeparam>
public class GenericRepo<T> : IGenericRepo<T> where T : class
{
    /// <summary>The shared, request-scoped database context.</summary>
    protected readonly ShopContext _context;

    /// <summary>
    /// Creates a new <see cref="GenericRepo{T}"/> over the supplied context.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public GenericRepo(ShopContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public IQueryable<T> Query()
    {
        // Return IQueryable (not a materialized list) so callers can append
        // Where/Skip/Take and only the final, paged query runs on the database.
        // Deferred execution is what keeps catalog pagination cheap — we never
        // load every row just to show one page.
        return _context.Set<T>();
    }

    /// <inheritdoc />
    public Task<List<T>> GetAllAsync()
    {
        return _context.Set<T>().ToListAsync();
    }

    /// <inheritdoc />
    public Task<T?> GetByIdAsync(int id)
    {
        return _context.Set<T>().FindAsync(id).AsTask();
    }

    /// <inheritdoc />
    public void Add(T entity)
    {
        _context.Set<T>().Add(entity);
    }

    /// <inheritdoc />
    public void Update(T entity)
    {
        _context.Set<T>().Update(entity);
    }

    /// <inheritdoc />
    public void Delete(T entity)
    {
        _context.Set<T>().Remove(entity);
    }
}
