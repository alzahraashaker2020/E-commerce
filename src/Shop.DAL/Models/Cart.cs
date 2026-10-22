namespace Shop.DAL.Models;

/// <summary>
/// A shopping cart. Schema-only in this scaffold: the cart is client-side and
/// no endpoint currently persists carts. Kept paired with <see cref="CartItem"/>
/// so a future server-side persisted cart can be added without a model change.
/// </summary>
public class Cart
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Opaque client cart token (GUID).</summary>
    public Guid CartToken { get; set; }

    /// <summary>UTC timestamp when the cart was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Marks rows created as demonstration data.</summary>
    public bool IsDemoData { get; set; }

    /// <summary>Lines in this cart.</summary>
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
