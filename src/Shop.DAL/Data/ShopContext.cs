using Microsoft.EntityFrameworkCore;
using Shop.DAL.Models;

namespace Shop.DAL.Data;

/// <summary>
/// Entity Framework Core database context for the online shopping platform.
/// Owns one <see cref="DbSet{TEntity}"/> per entity and configures keys,
/// relationships, unique indexes, decimal precision, and enum conversions.
/// Deterministic demo data is applied via <see cref="ShopContextSeed"/>.
/// </summary>
public class ShopContext : DbContext
{
    /// <summary>
    /// Creates a new <see cref="ShopContext"/> with the supplied options.
    /// </summary>
    /// <param name="options">Context options (provider, connection string).</param>
    public ShopContext(DbContextOptions<ShopContext> options)
        : base(options)
    {
    }

    /// <summary>Categories.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Products.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Product variants.</summary>
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    /// <summary>Product images.</summary>
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    /// <summary>Sizes (admin-managed variant dimension).</summary>
    public DbSet<Size> Sizes => Set<Size>();

    /// <summary>Colors (admin-managed variant dimension).</summary>
    public DbSet<Color> Colors => Set<Color>();

    /// <summary>Customers.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Carts.</summary>
    public DbSet<Cart> Carts => Set<Cart>();

    /// <summary>Cart items.</summary>
    public DbSet<CartItem> CartItems => Set<CartItem>();

    /// <summary>Orders.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Order items.</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <summary>Discounts.</summary>
    public DbSet<Discount> Discounts => Set<Discount>();

    /// <summary>Admin users.</summary>
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    /// <summary>RBAC roles.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Customer reviews.</summary>
    public DbSet<Review> Reviews => Set<Review>();

    /// <summary>Store-wide settings (single row).</summary>
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();
    /// <summary>Admin-managed currencies.</summary>
    public DbSet<Currency> Currencies => Set<Currency>();
    /// <summary>Admin-managed order status definitions.</summary>
    public DbSet<OrderStatusDefinition> OrderStatusDefinitions => Set<OrderStatusDefinition>();
    /// <summary>Delivery governorates.</summary>
    public DbSet<Governorate> Governorates => Set<Governorate>();
    /// <summary>Delivery areas and rates.</summary>
    public DbSet<DeliveryArea> DeliveryAreas => Set<DeliveryArea>();

    /// <summary>
    /// Configures the model: keys, relationships, unique indexes, decimal
    /// precision on money columns, enum-to-int conversions, then demo seeding.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Governorate>(e => { e.HasIndex(g => g.Name).IsUnique(); e.Property(g => g.Name).HasMaxLength(100); });
        modelBuilder.Entity<DeliveryArea>(e =>
        {
            e.HasIndex(a => new { a.GovernorateId, a.Name }).IsUnique();
            e.Property(a => a.Name).HasMaxLength(100);
            e.Property(a => a.ShippingFee).HasPrecision(18, 2);
            e.Property(a => a.CurrencyCode).HasMaxLength(10);
            e.HasOne(a => a.Governorate).WithMany().HasForeignKey(a => a.GovernorateId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Order>().HasOne<DeliveryArea>().WithMany().HasForeignKey(o => o.DeliveryAreaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Currency>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Code).HasMaxLength(10);
            e.Property(c => c.Name).HasMaxLength(100);
            e.Property(c => c.Symbol).HasMaxLength(8);
            e.HasData(new Currency { Id = 1, Code = "EGP", Name = "Egyptian Pound", Symbol = "E£" },
                new Currency { Id = 2, Code = "USD", Name = "US Dollar", Symbol = "$" });
        });
        modelBuilder.Entity<OrderStatusDefinition>(e =>
        {
            e.HasIndex(s => s.Name).IsUnique();
            e.Property(s => s.Name).HasMaxLength(100);
            foreach (OrderStatus behavior in Enum.GetValues<OrderStatus>())
                e.HasData(new OrderStatusDefinition { Id = (int)behavior + 1, Name = behavior.ToString(),
                    Behavior = behavior, DisplayOrder = (int)behavior, IsDefault = behavior == OrderStatus.Pending });
        });

        // --- Category (self-referencing two-level tree) ---
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.Slug).HasMaxLength(200);
            e.Property(c => c.ImageUrl).HasMaxLength(1000);

            // A category may have a parent; deleting a parent is restricted so
            // subcategories are never silently orphaned/cascaded away.
            e.HasOne(c => c.ParentCategory)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Product ---
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Slug).HasMaxLength(200);
            e.Property(p => p.BasePrice).HasPrecision(18, 2);
            e.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Size (admin-managed variant dimension) ---
        modelBuilder.Entity<Size>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(100);
            e.HasIndex(s => s.Name).IsUnique();
        });

        // --- Color (admin-managed variant dimension) ---
        modelBuilder.Entity<Color>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100);
            e.HasIndex(c => c.Name).IsUnique();
            e.Property(c => c.HexCode).HasMaxLength(9);
        });

        // --- ProductVariant ---
        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.Property(v => v.ImageUrls).HasConversion(
                images => System.Text.Json.JsonSerializer.Serialize(images, (System.Text.Json.JsonSerializerOptions?)null),
                json => System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                    (a, b) => a!.SequenceEqual(b!),
                    a => a.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
                    a => a.ToList()));
            e.HasKey(v => v.Id);
            e.Property(v => v.Sku).HasMaxLength(100);
            e.Property(v => v.Price).HasPrecision(18, 2);
            e.HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            // Size/Color are restricted on delete so a referenced attribute
            // cannot be silently removed (admin delete returns 409 instead).
            e.HasOne(v => v.Size)
                .WithMany(s => s.Variants)
                .HasForeignKey(v => v.SizeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.Color)
                .WithMany(c => c.Variants)
                .HasForeignKey(v => v.ColorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ProductImage ---
        modelBuilder.Entity<ProductImage>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Url).HasMaxLength(1000);
            e.HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Customer ---
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Email).HasMaxLength(256);
            e.Property(c => c.FullName).HasMaxLength(200);
            e.Property(c => c.ExternalProvider).HasMaxLength(40);
            e.Property(c => c.ExternalId).HasMaxLength(256);
            // Unique email for registered accounts (demo guest rows also have
            // distinct emails, so a simple unique index is safe here).
            e.HasIndex(c => c.Email).IsUnique();
        });

        // --- Review ---
        modelBuilder.Entity<Review>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Comment).HasMaxLength(2000);
            e.HasOne(r => r.Customer)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Cart / CartItem (schema-only in this scaffold) ---
        modelBuilder.Entity<Cart>(e =>
        {
            e.HasKey(c => c.Id);
        });
        modelBuilder.Entity<CartItem>(e =>
        {
            e.HasKey(ci => ci.Id);
            e.Property(ci => ci.UnitPriceSnapshot).HasPrecision(18, 2);
            e.HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ci => ci.Variant)
                .WithMany()
                .HasForeignKey(ci => ci.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Order ---
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.Property(o => o.OrderNumber).HasMaxLength(40);
            e.Property(o => o.Status).HasConversion<int>();
            e.Property(o => o.Status).IsConcurrencyToken();
            e.Property(o => o.StockDeducted).IsConcurrencyToken();
            e.Property(o => o.StatusDefinitionId).IsConcurrencyToken();
            e.HasOne(o => o.StatusDefinition).WithMany().HasForeignKey(o => o.StatusDefinitionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(o => o.PaymentMethod).HasConversion<int>();
            e.Property(o => o.PaymentStatus).HasConversion<int>();
            e.Property(o => o.Subtotal).HasPrecision(18, 2);
            e.Property(o => o.DiscountTotal).HasPrecision(18, 2);
            e.Property(o => o.Tax).HasPrecision(18, 2);
            e.Property(o => o.ShippingTotal).HasPrecision(18, 2);
            e.Property(o => o.Total).HasPrecision(18, 2);
            e.Property(o => o.CurrencyCode).HasMaxLength(10);
            e.Property(o => o.AddressLine1).HasMaxLength(200);
            e.Property(o => o.AddressLine2).HasMaxLength(200);
            e.Property(o => o.City).HasMaxLength(100);
            e.Property(o => o.StateRegion).HasMaxLength(100);
            e.Property(o => o.PostalCode).HasMaxLength(20);
            e.Property(o => o.Country).HasMaxLength(100);
            e.Property(o => o.ContactPhone).HasMaxLength(40);
            e.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- OrderItem ---
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(oi => oi.Id);
            e.Property(oi => oi.ProductNameSnapshot).HasMaxLength(200);
            e.Property(oi => oi.SizeSnapshot).HasMaxLength(100);
            e.Property(oi => oi.ColorSnapshot).HasMaxLength(100);
            e.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            e.Property(oi => oi.LineTotal).HasPrecision(18, 2);
            e.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(oi => oi.Variant)
                .WithMany()
                .HasForeignKey(oi => oi.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Discount ---
        modelBuilder.Entity<Discount>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.Code).IsUnique();
            e.Property(d => d.Code).HasMaxLength(40);
            e.Property(d => d.Type).HasConversion<int>();
            e.Property(d => d.Value).HasPrecision(18, 2);
        });

        // --- Role ---
        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).HasMaxLength(60);
        });

        // --- AdminUser ---
        modelBuilder.Entity<AdminUser>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.Username).IsUnique();
            e.Property(a => a.Username).HasMaxLength(60);
            e.Property(a => a.Email).HasMaxLength(256);
            e.HasOne(a => a.Role)
                .WithMany(r => r.AdminUsers)
                .HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- StoreSettings (single row, Id = 1) ---
        modelBuilder.Entity<StoreSettings>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.CurrencyCode).HasMaxLength(10);
            e.Property(s => s.CurrencySymbol).HasMaxLength(8);
        });

        // Apply deterministic demo seed data (HasData).
        ShopContextSeed.Seed(modelBuilder);
    }
}
