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

    /// <summary>Inventory items.</summary>
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

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

    /// <summary>
    /// Configures the model: keys, relationships, unique indexes, decimal
    /// precision on money columns, enum-to-int conversions, then demo seeding.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Category ---
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.Slug).HasMaxLength(200);
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

        // --- ProductVariant ---
        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Sku).HasMaxLength(100);
            e.Property(v => v.Price).HasPrecision(18, 2);
            e.Property(v => v.CompareAtPrice).HasPrecision(18, 2);
            e.HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
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

        // --- InventoryItem ---
        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasOne(i => i.Product)
                .WithMany(p => p.InventoryItems)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Variant)
                .WithMany()
                .HasForeignKey(i => i.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Customer ---
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Email).HasMaxLength(256);
            e.Property(c => c.FullName).HasMaxLength(200);
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
            e.Property(o => o.Subtotal).HasPrecision(18, 2);
            e.Property(o => o.DiscountTotal).HasPrecision(18, 2);
            e.Property(o => o.Tax).HasPrecision(18, 2);
            e.Property(o => o.ShippingTotal).HasPrecision(18, 2);
            e.Property(o => o.Total).HasPrecision(18, 2);
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

        // Apply deterministic demo seed data (HasData).
        ShopContextSeed.Seed(modelBuilder);
    }
}
