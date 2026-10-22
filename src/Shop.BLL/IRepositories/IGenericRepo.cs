namespace Shop.BLL.IRepositories;

/// <summary>
/// Generic repository contract shared by every entity repo. Read operations
/// that hit the database are asynchronous; <see cref="Add"/>/<see cref="Update"/>/
/// <see cref="Delete"/> only mutate the EF change tracker (no I/O) and stay
/// synchronous. The single commit point is the unit-of-work <c>Save()</c>.
/// </summary>
/// <typeparam name="T">The entity type managed by the repository.</typeparam>
public interface IGenericRepo<T> where T : class
{
    /// <summary>
    /// Returns a deferred <see cref="IQueryable{T}"/> so callers can compose
    /// filtering, paging, and includes before the query executes.
    /// </summary>
    IQueryable<T> Query();

    /// <summary>
    /// Asynchronously loads every row for the entity.
    /// </summary>
    Task<List<T>> GetAllAsync();

    /// <summary>
    /// Asynchronously loads a single entity by its primary key, or <c>null</c>
    /// when no row matches.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Marks a new entity for insertion (committed by the unit-of-work Save).
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    void Add(T entity);

    /// <summary>
    /// Marks an existing entity as modified (committed by the unit-of-work Save).
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(T entity);

    /// <summary>
    /// Marks a tracked entity for deletion (committed by the unit-of-work Save).
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    void Delete(T entity);
}
