namespace Shop.BLL.IRepositories;

/// <summary>
/// Unit-of-work contract exposing one repository per entity plus a single
/// commit point. All repos share the same request-scoped context, so a single
/// <see cref="Save"/> commits every pending change together.
/// </summary>
public interface IRepoWrapper
{
    /// <summary>Product repository.</summary>
    IProductRepo _Product { get; }

    /// <summary>Category repository.</summary>
    ICategoryRepo _Category { get; }

    /// <summary>Order repository.</summary>
    IOrderRepo _Order { get; }

    /// <summary>Customer repository.</summary>
    ICustomerRepo _Customer { get; }

    /// <summary>Discount repository.</summary>
    IDiscountRepo _Discount { get; }

    /// <summary>Size repository.</summary>
    ISizeRepo _Size { get; }

    /// <summary>Color repository.</summary>
    IColorRepo _Color { get; }

    /// <summary>Admin-user repository.</summary>
    IAdminUserRepo _AdminUser { get; }

    /// <summary>Role repository.</summary>
    IRoleRepo _Role { get; }

    /// <summary>Review repository.</summary>
    IReviewRepo _Review { get; }

    /// <summary>Store-settings repository.</summary>
    IStoreSettingsRepo _StoreSettings { get; }

    /// <summary>
    /// Cart repository. Present for structural symmetry only — unused by any
    /// current endpoint because the cart is client-side.
    /// </summary>
    ICartRepo _Cart { get; }

    /// <summary>
    /// Commits every pending change on the shared context in a single
    /// transaction (the one commit point for the unit of work).
    /// </summary>
    void Save();
}
