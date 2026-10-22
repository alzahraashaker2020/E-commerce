using Microsoft.EntityFrameworkCore;
using Shop.DAL.Models;

namespace Shop.DAL.Data;

/// <summary>
/// Applies deterministic demonstration data via EF Core <c>HasData</c>. Follows
/// strict <c>HasData</c> discipline so <c>dotnet ef migrations add</c> succeeds:
/// every row has a constant literal primary key in a disjoint range, FK scalars
/// only (no navigation graphs), fixed <see cref="DateTime"/> literals (never
/// <see cref="DateTime.UtcNow"/>), enums as fixed backing ints, and every row
/// is marked <c>IsDemoData = true</c>. The admin password hash is a precomputed
/// constant (plaintext documented for the demo), never a hasher call.
/// </summary>
public static class ShopContextSeed
{
    /// <summary>
    /// Plaintext of the demo admin password. The hash in <see cref="AdminPasswordHash"/>
    /// corresponds to this value. Documented for the demo only; never used in production.
    /// </summary>
    public const string AdminDemoPassword = "Admin@12345";

    /// <summary>
    /// Precomputed ASP.NET Core Identity (IdentityV3 compatibility mode)
    /// password hash for <see cref="AdminDemoPassword"/>. Produced by the same
    /// <c>PasswordHasher</c> the login path verifies with, so seed and verify
    /// never diverge. Embedded as a constant because <c>HasData</c> values must
    /// be deterministic.
    /// </summary>
    public const string AdminPasswordHash =
        "AQAAAAIAAYagAAAAEEFXRDAJVQm+ewFvN7Vr34QeNosx0FUZdw1WUhmkykFNRkcH5bjLM/2rbTxsBQ5hFw==";

    // Fixed UTC timestamps used throughout the seed (never DateTime.UtcNow).
    private static readonly DateTime SeedCreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SeedOrderPlacedAt1 = new DateTime(2024, 2, 10, 9, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime SeedOrderPlacedAt2 = new DateTime(2024, 2, 15, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SeedOrderPlacedAt3 = new DateTime(2024, 3, 1, 11, 15, 0, DateTimeKind.Utc);

    /// <summary>
    /// Registers all demo rows on the supplied <paramref name="modelBuilder"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder to seed.</param>
    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedRolesAndAdmin(modelBuilder);
        SeedCategories(modelBuilder);
        SeedProducts(modelBuilder);
        SeedVariants(modelBuilder);
        SeedImages(modelBuilder);
        SeedInventory(modelBuilder);
        SeedCustomers(modelBuilder);
        SeedDiscounts(modelBuilder);
        SeedOrders(modelBuilder);
        SeedOrderItems(modelBuilder);
    }

    // Roles (small ints) + one admin user.
    private static void SeedRolesAndAdmin(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" });

        modelBuilder.Entity<AdminUser>().HasData(
            new AdminUser
            {
                Id = 1,
                Username = "admin",
                Email = "admin@onlineshopping.demo",
                PasswordHash = AdminPasswordHash,
                RoleId = 1,
                IsActive = true,
                IsDemoData = true
            });
    }

    // Categories: IDs 1-9.
    private static void SeedCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Apparel", Slug = "apparel", Description = "Clothing and wearables.", IsDemoData = true },
            new Category { Id = 2, Name = "Footwear", Slug = "footwear", Description = "Shoes and sandals.", IsDemoData = true },
            new Category { Id = 3, Name = "Accessories", Slug = "accessories", Description = "Bags, belts, and more.", IsDemoData = true },
            new Category { Id = 4, Name = "Electronics", Slug = "electronics", Description = "Gadgets and devices.", IsDemoData = true });
    }

    // Products: IDs in the 100-series. 12 products across the 4 categories.
    private static void SeedProducts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 101, Name = "Classic Tee", Slug = "classic-tee", DescriptionHtml = "<p>Soft cotton T-shirt.</p>", Brand = "Everyday", BasePrice = 19.99m, CategoryId = 1, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 102, Name = "Hooded Sweatshirt", Slug = "hooded-sweatshirt", DescriptionHtml = "<p>Cozy fleece hoodie.</p>", Brand = "Everyday", BasePrice = 44.99m, CategoryId = 1, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 103, Name = "Denim Jacket", Slug = "denim-jacket", DescriptionHtml = "<p>Rugged denim jacket.</p>", Brand = "BlueLine", BasePrice = 79.99m, CategoryId = 1, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 104, Name = "Running Shoes", Slug = "running-shoes", DescriptionHtml = "<p>Lightweight running shoes.</p>", Brand = "Strideo", BasePrice = 89.99m, CategoryId = 2, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 105, Name = "Leather Boots", Slug = "leather-boots", DescriptionHtml = "<p>Durable leather boots.</p>", Brand = "TrailMark", BasePrice = 129.99m, CategoryId = 2, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 106, Name = "Canvas Sneakers", Slug = "canvas-sneakers", DescriptionHtml = "<p>Casual canvas sneakers.</p>", Brand = "Strideo", BasePrice = 49.99m, CategoryId = 2, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 107, Name = "Leather Belt", Slug = "leather-belt", DescriptionHtml = "<p>Full-grain leather belt.</p>", Brand = "TrailMark", BasePrice = 34.99m, CategoryId = 3, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 108, Name = "Tote Bag", Slug = "tote-bag", DescriptionHtml = "<p>Spacious canvas tote.</p>", Brand = "Everyday", BasePrice = 24.99m, CategoryId = 3, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 109, Name = "Wool Scarf", Slug = "wool-scarf", DescriptionHtml = "<p>Warm wool scarf.</p>", Brand = "BlueLine", BasePrice = 29.99m, CategoryId = 3, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 110, Name = "Wireless Earbuds", Slug = "wireless-earbuds", DescriptionHtml = "<p>Noise-isolating earbuds.</p>", Brand = "SoundCore", BasePrice = 59.99m, CategoryId = 4, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 111, Name = "Smart Watch", Slug = "smart-watch", DescriptionHtml = "<p>Fitness-tracking smart watch.</p>", Brand = "SoundCore", BasePrice = 149.99m, CategoryId = 4, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 112, Name = "Portable Charger", Slug = "portable-charger", DescriptionHtml = "<p>10,000mAh power bank.</p>", Brand = "SoundCore", BasePrice = 39.99m, CategoryId = 4, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true });
    }

    // Variants: IDs in the 1000-series. 1-3 variants for several products.
    private static void SeedVariants(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductVariant>().HasData(
            // Classic Tee - sizes
            new ProductVariant { Id = 1001, ProductId = 101, Sku = "TEE-S", OptionName = "Size", OptionValue = "S", Price = 19.99m, IsDemoData = true },
            new ProductVariant { Id = 1002, ProductId = 101, Sku = "TEE-M", OptionName = "Size", OptionValue = "M", Price = 19.99m, IsDemoData = true },
            new ProductVariant { Id = 1003, ProductId = 101, Sku = "TEE-L", OptionName = "Size", OptionValue = "L", Price = 19.99m, IsDemoData = true },
            // Hooded Sweatshirt - sizes
            new ProductVariant { Id = 1004, ProductId = 102, Sku = "HOOD-M", OptionName = "Size", OptionValue = "M", Price = 44.99m, CompareAtPrice = 54.99m, IsDemoData = true },
            new ProductVariant { Id = 1005, ProductId = 102, Sku = "HOOD-L", OptionName = "Size", OptionValue = "L", Price = 44.99m, CompareAtPrice = 54.99m, IsDemoData = true },
            // Running Shoes - sizes
            new ProductVariant { Id = 1006, ProductId = 104, Sku = "RUN-9", OptionName = "Size", OptionValue = "9", Price = 89.99m, IsDemoData = true },
            new ProductVariant { Id = 1007, ProductId = 104, Sku = "RUN-10", OptionName = "Size", OptionValue = "10", Price = 89.99m, IsDemoData = true },
            new ProductVariant { Id = 1008, ProductId = 104, Sku = "RUN-11", OptionName = "Size", OptionValue = "11", Price = 89.99m, IsDemoData = true },
            // Leather Boots - sizes
            new ProductVariant { Id = 1009, ProductId = 105, Sku = "BOOT-9", OptionName = "Size", OptionValue = "9", Price = 129.99m, IsDemoData = true },
            new ProductVariant { Id = 1010, ProductId = 105, Sku = "BOOT-10", OptionName = "Size", OptionValue = "10", Price = 129.99m, IsDemoData = true },
            // Wireless Earbuds - color
            new ProductVariant { Id = 1011, ProductId = 110, Sku = "EAR-BLK", OptionName = "Color", OptionValue = "Black", Price = 59.99m, IsDemoData = true },
            new ProductVariant { Id = 1012, ProductId = 110, Sku = "EAR-WHT", OptionName = "Color", OptionValue = "White", Price = 59.99m, IsDemoData = true });
    }

    // Images: IDs in the 2000-series. 1-2 images per product.
    private static void SeedImages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductImage>().HasData(
            new ProductImage { Id = 2001, ProductId = 101, Url = "https://demo.local/img/classic-tee-1.jpg", AltText = "Classic Tee front", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2002, ProductId = 101, Url = "https://demo.local/img/classic-tee-2.jpg", AltText = "Classic Tee back", SortOrder = 1, IsDemoData = true },
            new ProductImage { Id = 2003, ProductId = 102, Url = "https://demo.local/img/hoodie-1.jpg", AltText = "Hooded Sweatshirt", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2004, ProductId = 103, Url = "https://demo.local/img/denim-jacket-1.jpg", AltText = "Denim Jacket", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2005, ProductId = 104, Url = "https://demo.local/img/running-shoes-1.jpg", AltText = "Running Shoes", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2006, ProductId = 105, Url = "https://demo.local/img/leather-boots-1.jpg", AltText = "Leather Boots", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2007, ProductId = 106, Url = "https://demo.local/img/canvas-sneakers-1.jpg", AltText = "Canvas Sneakers", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2008, ProductId = 107, Url = "https://demo.local/img/leather-belt-1.jpg", AltText = "Leather Belt", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2009, ProductId = 108, Url = "https://demo.local/img/tote-bag-1.jpg", AltText = "Tote Bag", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2010, ProductId = 109, Url = "https://demo.local/img/wool-scarf-1.jpg", AltText = "Wool Scarf", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2011, ProductId = 110, Url = "https://demo.local/img/earbuds-1.jpg", AltText = "Wireless Earbuds", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2012, ProductId = 111, Url = "https://demo.local/img/smart-watch-1.jpg", AltText = "Smart Watch", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2013, ProductId = 112, Url = "https://demo.local/img/charger-1.jpg", AltText = "Portable Charger", SortOrder = 0, IsDemoData = true });
    }

    // Inventory: IDs in the 3000-series. Variant-level rows where variants exist,
    // else product-level. At least one row is at/below its LowStockThreshold.
    private static void SeedInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>().HasData(
            // Classic Tee variants
            new InventoryItem { Id = 3001, ProductId = 101, VariantId = 1001, QuantityOnHand = 50, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3002, ProductId = 101, VariantId = 1002, QuantityOnHand = 3, LowStockThreshold = 5, IsDemoData = true }, // LOW STOCK
            new InventoryItem { Id = 3003, ProductId = 101, VariantId = 1003, QuantityOnHand = 40, LowStockThreshold = 5, IsDemoData = true },
            // Hooded Sweatshirt variants
            new InventoryItem { Id = 3004, ProductId = 102, VariantId = 1004, QuantityOnHand = 20, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3005, ProductId = 102, VariantId = 1005, QuantityOnHand = 2, LowStockThreshold = 5, IsDemoData = true }, // LOW STOCK
            // Denim Jacket - product-level (no variants)
            new InventoryItem { Id = 3006, ProductId = 103, VariantId = null, QuantityOnHand = 15, LowStockThreshold = 4, IsDemoData = true },
            // Running Shoes variants
            new InventoryItem { Id = 3007, ProductId = 104, VariantId = 1006, QuantityOnHand = 25, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3008, ProductId = 104, VariantId = 1007, QuantityOnHand = 30, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3009, ProductId = 104, VariantId = 1008, QuantityOnHand = 5, LowStockThreshold = 5, IsDemoData = true }, // LOW STOCK (at threshold)
            // Leather Boots variants
            new InventoryItem { Id = 3010, ProductId = 105, VariantId = 1009, QuantityOnHand = 12, LowStockThreshold = 3, IsDemoData = true },
            new InventoryItem { Id = 3011, ProductId = 105, VariantId = 1010, QuantityOnHand = 8, LowStockThreshold = 3, IsDemoData = true },
            // Remaining product-level rows
            new InventoryItem { Id = 3012, ProductId = 106, VariantId = null, QuantityOnHand = 60, LowStockThreshold = 6, IsDemoData = true },
            new InventoryItem { Id = 3013, ProductId = 107, VariantId = null, QuantityOnHand = 45, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3014, ProductId = 108, VariantId = null, QuantityOnHand = 70, LowStockThreshold = 6, IsDemoData = true },
            new InventoryItem { Id = 3015, ProductId = 109, VariantId = null, QuantityOnHand = 4, LowStockThreshold = 5, IsDemoData = true }, // LOW STOCK
            // Earbuds variants
            new InventoryItem { Id = 3016, ProductId = 110, VariantId = 1011, QuantityOnHand = 35, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3017, ProductId = 110, VariantId = 1012, QuantityOnHand = 22, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3018, ProductId = 111, VariantId = null, QuantityOnHand = 18, LowStockThreshold = 4, IsDemoData = true },
            new InventoryItem { Id = 3019, ProductId = 112, VariantId = null, QuantityOnHand = 50, LowStockThreshold = 6, IsDemoData = true });
    }

    // Customers: small ints (reused as FK on orders).
    private static void SeedCustomers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, Email = "jordan@demo.local", FullName = "Jordan Rivera", Phone = "+1-555-0100", MarketingConsent = true, IsDemoData = true },
            new Customer { Id = 2, Email = "sam@demo.local", FullName = "Sam Taylor", Phone = "+1-555-0101", MarketingConsent = false, IsDemoData = true });
    }

    // Discounts: small ints.
    private static void SeedDiscounts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Discount>().HasData(
            new Discount { Id = 1, Code = "WELCOME10", Type = DiscountType.Percentage, Value = 10m, IsActive = true, StartsAt = SeedCreatedAt, EndsAt = null, IsDemoData = true },
            new Discount { Id = 2, Code = "SAVE5", Type = DiscountType.Fixed, Value = 5m, IsActive = true, StartsAt = SeedCreatedAt, EndsAt = null, IsDemoData = true });
    }

    // Orders: IDs in the 5000-series. 3 orders; >=1 placed; one uses a low-stock product.
    private static void SeedOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasData(
            // Placed (Paid) order for a registered customer, includes low-stock Classic Tee (M).
            new Order { Id = 5001, OrderNumber = "ORD-20240210-A1B2C3", CustomerId = 1, GuestEmail = null, GuestName = null, Status = OrderStatus.Paid, Subtotal = 64.98m, DiscountTotal = 6.50m, Tax = 0m, ShippingTotal = 0m, Total = 58.48m, PlacedAt = SeedOrderPlacedAt1, IsDemoData = true },
            // Guest order, delivered.
            new Order { Id = 5002, OrderNumber = "ORD-20240215-D4E5F6", CustomerId = null, GuestEmail = "guest@demo.local", GuestName = "Guest Shopper", Status = OrderStatus.Delivered, Subtotal = 89.99m, DiscountTotal = 0m, Tax = 0m, ShippingTotal = 7.50m, Total = 97.49m, PlacedAt = SeedOrderPlacedAt2, IsDemoData = true },
            // Pending order for the second customer.
            new Order { Id = 5003, OrderNumber = "ORD-20240301-G7H8J9", CustomerId = 2, GuestEmail = null, GuestName = null, Status = OrderStatus.Pending, Subtotal = 39.99m, DiscountTotal = 0m, Tax = 0m, ShippingTotal = 5.00m, Total = 44.99m, PlacedAt = SeedOrderPlacedAt3, IsDemoData = true });
    }

    // Order items: IDs in the 6000-series. FK scalars only.
    private static void SeedOrderItems(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>().HasData(
            // Order 5001: Classic Tee (M, low stock) x2 + Tote Bag x1
            new OrderItem { Id = 6001, OrderId = 5001, ProductId = 101, VariantId = 1002, ProductNameSnapshot = "Classic Tee", UnitPrice = 19.99m, Quantity = 2, LineTotal = 39.98m, IsDemoData = true },
            new OrderItem { Id = 6002, OrderId = 5001, ProductId = 108, VariantId = null, ProductNameSnapshot = "Tote Bag", UnitPrice = 24.99m, Quantity = 1, LineTotal = 24.99m, IsDemoData = true },
            // Order 5002: Running Shoes (size 11) x1
            new OrderItem { Id = 6003, OrderId = 5002, ProductId = 104, VariantId = 1008, ProductNameSnapshot = "Running Shoes", UnitPrice = 89.99m, Quantity = 1, LineTotal = 89.99m, IsDemoData = true },
            // Order 5003: Portable Charger x1
            new OrderItem { Id = 6004, OrderId = 5003, ProductId = 112, VariantId = null, ProductNameSnapshot = "Portable Charger", UnitPrice = 39.99m, Quantity = 1, LineTotal = 39.99m, IsDemoData = true });
    }
}
