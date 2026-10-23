using Shop.BLL.IRepositories;
using Shop.DAL.Data;

namespace Shop.BLL.Repositories;

/// <summary>
/// Unit-of-work implementation. Holds a single request-scoped
/// <see cref="ShopContext"/> and lazily creates each per-entity repo on first
/// access. Because all repos share the one context, a single <see cref="Save"/>
/// commits every pending change together.
/// </summary>
public class RepoWrapper : IRepoWrapper
{
    private readonly ShopContext _context;

    // One nullable backing field per repo; each is created lazily below.
    private IProductRepo? _productRepo;
    private ICategoryRepo? _categoryRepo;
    private IOrderRepo? _orderRepo;
    private ICustomerRepo? _customerRepo;
    private IDiscountRepo? _discountRepo;
    private IInventoryItemRepo? _inventoryItemRepo;
    private IAdminUserRepo? _adminUserRepo;
    private IRoleRepo? _roleRepo;
    private ICartRepo? _cartRepo;
    private IReviewRepo? _reviewRepo;
    private IStoreSettingsRepo? _storeSettingsRepo;

    /// <summary>
    /// Creates a new <see cref="RepoWrapper"/> over the supplied context.
    /// </summary>
    /// <param name="context">The request-scoped <see cref="ShopContext"/>.</param>
    public RepoWrapper(ShopContext context)
    {
        _context = context;
    }

    // Lazy init: create the repo only on first access, then reuse the same
    // instance for the lifetime of this unit of work (one per request).
    /// <inheritdoc />
    public IProductRepo _Product => _productRepo ??= new ProductRepo(_context);

    /// <inheritdoc />
    public ICategoryRepo _Category => _categoryRepo ??= new CategoryRepo(_context);

    /// <inheritdoc />
    public IOrderRepo _Order => _orderRepo ??= new OrderRepo(_context);

    /// <inheritdoc />
    public ICustomerRepo _Customer => _customerRepo ??= new CustomerRepo(_context);

    /// <inheritdoc />
    public IDiscountRepo _Discount => _discountRepo ??= new DiscountRepo(_context);

    /// <inheritdoc />
    public IInventoryItemRepo _InventoryItem => _inventoryItemRepo ??= new InventoryItemRepo(_context);

    /// <inheritdoc />
    public IAdminUserRepo _AdminUser => _adminUserRepo ??= new AdminUserRepo(_context);

    /// <inheritdoc />
    public IRoleRepo _Role => _roleRepo ??= new RoleRepo(_context);

    /// <inheritdoc />
    public ICartRepo _Cart => _cartRepo ??= new CartRepo(_context);

    /// <inheritdoc />
    public IReviewRepo _Review => _reviewRepo ??= new ReviewRepo(_context);

    /// <inheritdoc />
    public IStoreSettingsRepo _StoreSettings => _storeSettingsRepo ??= new StoreSettingsRepo(_context);

    /// <inheritdoc />
    public void Save()
    {
        // Single commit point for the unit of work: one SaveChanges() flushes
        // every pending change tracked across the shared context.
        _context.SaveChanges();
    }
}
