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

    /// <summary>
    /// Plaintext of the demo REGISTERED customer password (customer 1,
    /// jordan@demo.local). For the demo this intentionally equals
    /// <see cref="AdminDemoPassword"/> so the seeded customer can reuse the same
    /// verified IdentityV3 hash (<see cref="AdminPasswordHash"/>) — a constant
    /// is required by <c>HasData</c>, and reusing a known-good hash guarantees
    /// the login actually verifies. Documented for the demo only.
    /// </summary>
    public const string CustomerDemoPassword = AdminDemoPassword;

    /// <summary>
    /// Password hash for the demo registered customer. Reuses the verified
    /// <see cref="AdminPasswordHash"/> (same password, same hasher/options) so
    /// customer login verification succeeds deterministically.
    /// </summary>
    public const string CustomerPasswordHash = AdminPasswordHash;

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
        SeedReviews(modelBuilder);
        SeedStoreSettings(modelBuilder);
    }

    // Store settings: a single demo row (Id = 1) priced in EGP.
    private static void SeedStoreSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoreSettings>().HasData(
            new StoreSettings { Id = 1, CurrencyCode = "EGP", CurrencySymbol = "E\u00a3", IsDemoData = true });
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

    // Categories: a two-level home-furnishings tree.
    //   Top-level (ParentCategoryId = null): IDs 1-4.
    //   Subcategories (ParentCategoryId set): IDs 11+.
    // Image URLs point at the Angular assets copied from the reference photos.
    private static void SeedCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            // --- Top-level categories ---
            new Category { Id = 1, Name = "Sofa Styling", Slug = "sofa-styling", Description = "Covers, throws, and cushions for your sofa.", ImageUrl = "assets/images/categories/sofa.webp", ParentCategoryId = null, IsDemoData = true },
            new Category { Id = 2, Name = "Wall Styling", Slug = "wall-styling", Description = "Macramé, hangings, and wall décor.", ImageUrl = "assets/images/categories/wall.webp", ParentCategoryId = null, IsDemoData = true },
            new Category { Id = 3, Name = "Floor Styling", Slug = "floor-styling", Description = "Rugs, runners, and floor accents.", ImageUrl = "assets/images/categories/floor.webp", ParentCategoryId = null, IsDemoData = true },
            new Category { Id = 4, Name = "New Arrivals", Slug = "new-arrivals", Description = "The latest additions to the collection.", ImageUrl = "assets/images/categories/newarrivals.webp", ParentCategoryId = null, IsDemoData = true },

            // --- Sofa Styling subcategories (parent 1) ---
            new Category { Id = 11, Name = "Sofa covers", Slug = "sofa-covers", Description = "Protective, elegant sofa covers.", ImageUrl = "assets/images/sofa/sofa-covers.webp", ParentCategoryId = 1, IsDemoData = true },
            new Category { Id = 12, Name = "Blanket / throw out", Slug = "blanket-throw", Description = "Soft throws and blankets.", ImageUrl = "assets/images/sofa/blanket-throw.webp", ParentCategoryId = 1, IsDemoData = true },
            new Category { Id = 13, Name = "Hand made sets", Slug = "hand-made-sets", Description = "Handcrafted cushion and cover sets.", ImageUrl = "assets/images/sofa/handmade-sets.webp", ParentCategoryId = 1, IsDemoData = true },
            new Category { Id = 14, Name = "Printed sets", Slug = "printed-sets", Description = "Patterned, printed cushion sets.", ImageUrl = "assets/images/sofa/printed-sets.webp", ParentCategoryId = 1, IsDemoData = true },

            // --- Wall Styling subcategories (parent 2) ---
            new Category { Id = 21, Name = "Macramé hangings", Slug = "macrame-hangings", Description = "Woven macramé wall hangings.", ImageUrl = "assets/images/categories/wall.webp", ParentCategoryId = 2, IsDemoData = true },
            new Category { Id = 22, Name = "Wall frames", Slug = "wall-frames", Description = "Decorative wall frames.", ImageUrl = "assets/images/categories/wall.webp", ParentCategoryId = 2, IsDemoData = true },

            // --- Floor Styling subcategories (parent 3) ---
            new Category { Id = 31, Name = "Area rugs", Slug = "area-rugs", Description = "Soft area rugs.", ImageUrl = "assets/images/categories/floor.webp", ParentCategoryId = 3, IsDemoData = true },
            new Category { Id = 32, Name = "Runners", Slug = "runners", Description = "Hallway and kitchen runners.", ImageUrl = "assets/images/categories/floor.webp", ParentCategoryId = 3, IsDemoData = true },

            // --- New Arrivals subcategory (parent 4) ---
            new Category { Id = 41, Name = "Latest picks", Slug = "latest-picks", Description = "Freshly added items.", ImageUrl = "assets/images/categories/newarrivals.webp", ParentCategoryId = 4, IsDemoData = true });
    }

    // Products: IDs in the 100-series. Home-furnishing items attached to the
    // leaf (subcategory) level. Image URLs reuse the copied reference photos.
    private static void SeedProducts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(
            // Sofa covers (cat 11)
            new Product { Id = 101, Name = "Woven Sofa Cover", Slug = "woven-sofa-cover", DescriptionHtml = "<p>Soft woven cover that drapes any sofa in elegance.</p>", Brand = "Makani", BasePrice = 59.99m, CategoryId = 11, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 102, Name = "Stretch Slipcover", Slug = "stretch-slipcover", DescriptionHtml = "<p>Form-fitting stretch slipcover for a tailored look.</p>", Brand = "Makani", BasePrice = 74.99m, CategoryId = 11, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            // Blanket / throw out (cat 12)
            new Product { Id = 103, Name = "Tasseled Throw Blanket", Slug = "tasseled-throw-blanket", DescriptionHtml = "<p>Cozy tasseled throw in warm neutral tones.</p>", Brand = "Makani", BasePrice = 39.99m, CategoryId = 12, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 104, Name = "Knitted Throw", Slug = "knitted-throw", DescriptionHtml = "<p>Chunky knitted throw for the sofa or bed.</p>", Brand = "Makani", BasePrice = 44.99m, CategoryId = 12, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            // Hand made sets (cat 13)
            new Product { Id = 105, Name = "Handmade Cushion Set", Slug = "handmade-cushion-set", DescriptionHtml = "<p>Set of handcrafted textured cushions.</p>", Brand = "Makani", BasePrice = 69.99m, CategoryId = 13, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 106, Name = "Artisan Pillow Trio", Slug = "artisan-pillow-trio", DescriptionHtml = "<p>Three artisan pillows in complementary weaves.</p>", Brand = "Makani", BasePrice = 84.99m, CategoryId = 13, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            // Printed sets (cat 14)
            new Product { Id = 107, Name = "Printed Cushion Set", Slug = "printed-cushion-set", DescriptionHtml = "<p>Patterned printed cushion set to accent any sofa.</p>", Brand = "Makani", BasePrice = 54.99m, CategoryId = 14, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 108, Name = "Floral Print Pillows", Slug = "floral-print-pillows", DescriptionHtml = "<p>Pair of floral-print pillows.</p>", Brand = "Makani", BasePrice = 42.99m, CategoryId = 14, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            // Wall styling (cat 21 / 22)
            new Product { Id = 109, Name = "Macramé Wall Hanging", Slug = "macrame-wall-hanging", DescriptionHtml = "<p>Hand-knotted macramé wall hanging.</p>", Brand = "Makani", BasePrice = 49.99m, CategoryId = 21, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 110, Name = "Boho Wall Frame", Slug = "boho-wall-frame", DescriptionHtml = "<p>Decorative boho wall frame.</p>", Brand = "Makani", BasePrice = 34.99m, CategoryId = 22, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            // Floor styling (cat 31 / 32)
            new Product { Id = 111, Name = "Textured Area Rug", Slug = "textured-area-rug", DescriptionHtml = "<p>Soft textured area rug in neutral tones.</p>", Brand = "Makani", BasePrice = 119.99m, CategoryId = 31, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Product { Id = 112, Name = "Woven Floor Runner", Slug = "woven-floor-runner", DescriptionHtml = "<p>Durable woven runner for hallways.</p>", Brand = "Makani", BasePrice = 64.99m, CategoryId = 32, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            // New arrivals (cat 41)
            new Product { Id = 113, Name = "Autumn Collection Throw", Slug = "autumn-collection-throw", DescriptionHtml = "<p>New-season throw in autumn hues.</p>", Brand = "Makani", BasePrice = 47.99m, CategoryId = 41, IsActive = true, CreatedAt = SeedCreatedAt, IsDemoData = true });
    }

    // Variants: IDs in the 1000-series. Home goods use size/colour options
    // where sensible (covers by sofa size, rugs by dimension, cushions by colour).
    private static void SeedVariants(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductVariant>().HasData(
            // Woven Sofa Cover - sofa size
            new ProductVariant { Id = 1001, ProductId = 101, Sku = "COVER-2S", OptionName = "Size", OptionValue = "2-Seater", Price = 59.99m, IsDemoData = true },
            new ProductVariant { Id = 1002, ProductId = 101, Sku = "COVER-3S", OptionName = "Size", OptionValue = "3-Seater", Price = 69.99m, IsDemoData = true },
            new ProductVariant { Id = 1003, ProductId = 101, Sku = "COVER-LS", OptionName = "Size", OptionValue = "L-Shape", Price = 89.99m, IsDemoData = true },
            // Stretch Slipcover - sofa size
            new ProductVariant { Id = 1004, ProductId = 102, Sku = "SLIP-2S", OptionName = "Size", OptionValue = "2-Seater", Price = 74.99m, CompareAtPrice = 84.99m, IsDemoData = true },
            new ProductVariant { Id = 1005, ProductId = 102, Sku = "SLIP-3S", OptionName = "Size", OptionValue = "3-Seater", Price = 84.99m, CompareAtPrice = 94.99m, IsDemoData = true },
            // Handmade Cushion Set - colour
            new ProductVariant { Id = 1006, ProductId = 105, Sku = "CUSH-BEIGE", OptionName = "Colour", OptionValue = "Beige", Price = 69.99m, IsDemoData = true },
            new ProductVariant { Id = 1007, ProductId = 105, Sku = "CUSH-CLAY", OptionName = "Colour", OptionValue = "Clay", Price = 69.99m, IsDemoData = true },
            // Printed Cushion Set - colour
            new ProductVariant { Id = 1008, ProductId = 107, Sku = "PRNT-FLORAL", OptionName = "Pattern", OptionValue = "Floral", Price = 54.99m, IsDemoData = true },
            new ProductVariant { Id = 1009, ProductId = 107, Sku = "PRNT-GEO", OptionName = "Pattern", OptionValue = "Geometric", Price = 54.99m, IsDemoData = true },
            // Textured Area Rug - dimension
            new ProductVariant { Id = 1010, ProductId = 111, Sku = "RUG-120", OptionName = "Size", OptionValue = "120x170", Price = 119.99m, IsDemoData = true },
            new ProductVariant { Id = 1011, ProductId = 111, Sku = "RUG-160", OptionName = "Size", OptionValue = "160x230", Price = 159.99m, IsDemoData = true },
            new ProductVariant { Id = 1012, ProductId = 111, Sku = "RUG-200", OptionName = "Size", OptionValue = "200x290", Price = 199.99m, IsDemoData = true });
    }

    // Images: IDs in the 2000-series. URLs point at the Angular assets copied
    // from the reference photos (served under /assets by the client build).
    private static void SeedImages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductImage>().HasData(
            new ProductImage { Id = 2001, ProductId = 101, Url = "assets/images/sofa/sofa-covers.webp", AltText = "Woven Sofa Cover", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2002, ProductId = 102, Url = "assets/images/sofa/sofa-covers.webp", AltText = "Stretch Slipcover", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2003, ProductId = 103, Url = "assets/images/sofa/blanket-throw.webp", AltText = "Tasseled Throw Blanket", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2004, ProductId = 104, Url = "assets/images/sofa/blanket-throw.webp", AltText = "Knitted Throw", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2005, ProductId = 105, Url = "assets/images/sofa/handmade-sets.webp", AltText = "Handmade Cushion Set", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2006, ProductId = 106, Url = "assets/images/sofa/handmade-sets.webp", AltText = "Artisan Pillow Trio", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2007, ProductId = 107, Url = "assets/images/sofa/printed-sets.webp", AltText = "Printed Cushion Set", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2008, ProductId = 108, Url = "assets/images/sofa/printed-sets.webp", AltText = "Floral Print Pillows", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2009, ProductId = 109, Url = "assets/images/categories/wall.webp", AltText = "Macramé Wall Hanging", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2010, ProductId = 110, Url = "assets/images/categories/wall.webp", AltText = "Boho Wall Frame", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2011, ProductId = 111, Url = "assets/images/categories/floor.webp", AltText = "Textured Area Rug", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2012, ProductId = 112, Url = "assets/images/categories/floor.webp", AltText = "Woven Floor Runner", SortOrder = 0, IsDemoData = true },
            new ProductImage { Id = 2013, ProductId = 113, Url = "assets/images/categories/newarrivals.webp", AltText = "Autumn Collection Throw", SortOrder = 0, IsDemoData = true });
    }

    // Inventory: IDs in the 3000-series. Variant-level rows where variants exist,
    // else product-level. At least one row is at/below its LowStockThreshold.
    private static void SeedInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>().HasData(
            // Woven Sofa Cover variants
            new InventoryItem { Id = 3001, ProductId = 101, VariantId = 1001, QuantityOnHand = 40, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3002, ProductId = 101, VariantId = 1002, QuantityOnHand = 3, LowStockThreshold = 5, IsDemoData = true }, // LOW STOCK
            new InventoryItem { Id = 3003, ProductId = 101, VariantId = 1003, QuantityOnHand = 18, LowStockThreshold = 5, IsDemoData = true },
            // Stretch Slipcover variants
            new InventoryItem { Id = 3004, ProductId = 102, VariantId = 1004, QuantityOnHand = 22, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3005, ProductId = 102, VariantId = 1005, QuantityOnHand = 2, LowStockThreshold = 5, IsDemoData = true }, // LOW STOCK
            // Throws - product-level (no variants)
            new InventoryItem { Id = 3006, ProductId = 103, VariantId = null, QuantityOnHand = 55, LowStockThreshold = 6, IsDemoData = true },
            new InventoryItem { Id = 3007, ProductId = 104, VariantId = null, QuantityOnHand = 30, LowStockThreshold = 5, IsDemoData = true },
            // Handmade Cushion Set variants
            new InventoryItem { Id = 3008, ProductId = 105, VariantId = 1006, QuantityOnHand = 14, LowStockThreshold = 4, IsDemoData = true },
            new InventoryItem { Id = 3009, ProductId = 105, VariantId = 1007, QuantityOnHand = 4, LowStockThreshold = 4, IsDemoData = true }, // LOW STOCK (at threshold)
            // Artisan Pillow Trio - product-level
            new InventoryItem { Id = 3010, ProductId = 106, VariantId = null, QuantityOnHand = 16, LowStockThreshold = 4, IsDemoData = true },
            // Printed Cushion Set variants
            new InventoryItem { Id = 3011, ProductId = 107, VariantId = 1008, QuantityOnHand = 26, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3012, ProductId = 107, VariantId = 1009, QuantityOnHand = 20, LowStockThreshold = 5, IsDemoData = true },
            // Remaining product-level rows
            new InventoryItem { Id = 3013, ProductId = 108, VariantId = null, QuantityOnHand = 38, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3014, ProductId = 109, VariantId = null, QuantityOnHand = 12, LowStockThreshold = 4, IsDemoData = true },
            new InventoryItem { Id = 3015, ProductId = 110, VariantId = null, QuantityOnHand = 4, LowStockThreshold = 5, IsDemoData = true }, // LOW STOCK
            // Area Rug variants
            new InventoryItem { Id = 3016, ProductId = 111, VariantId = 1010, QuantityOnHand = 15, LowStockThreshold = 4, IsDemoData = true },
            new InventoryItem { Id = 3017, ProductId = 111, VariantId = 1011, QuantityOnHand = 9, LowStockThreshold = 4, IsDemoData = true },
            new InventoryItem { Id = 3018, ProductId = 111, VariantId = 1012, QuantityOnHand = 6, LowStockThreshold = 4, IsDemoData = true },
            // Floor Runner + New Arrival - product-level
            new InventoryItem { Id = 3019, ProductId = 112, VariantId = null, QuantityOnHand = 28, LowStockThreshold = 5, IsDemoData = true },
            new InventoryItem { Id = 3020, ProductId = 113, VariantId = null, QuantityOnHand = 33, LowStockThreshold = 5, IsDemoData = true });
    }

    // Customers: small ints (reused as FK on orders). Customer 1 is a demo
    // REGISTERED account (login: jordan@demo.local / the demo password below);
    // customer 2 is a profile-only guest-style row.
    private static void SeedCustomers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, Email = "jordan@demo.local", FullName = "Jordan Rivera", Phone = "+1-555-0100", MarketingConsent = true, PasswordHash = CustomerPasswordHash, IsRegistered = true, CreatedAt = SeedCreatedAt, IsDemoData = true },
            new Customer { Id = 2, Email = "sam@demo.local", FullName = "Sam Taylor", Phone = "+1-555-0101", MarketingConsent = false, PasswordHash = null, IsRegistered = false, CreatedAt = SeedCreatedAt, IsDemoData = true });
    }

    // Reviews: IDs in the 7000-series. Seeded for the registered customer on
    // products they purchased (order 5001), demonstrating purchase-gated reviews.
    private static void SeedReviews(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>().HasData(
            new Review { Id = 7001, CustomerId = 1, ProductId = 101, OrderId = 5001, Rating = 5, Comment = "The woven cover transformed our sofa — beautiful quality.", CreatedAt = SeedOrderPlacedAt1, IsDemoData = true },
            new Review { Id = 7002, CustomerId = 1, ProductId = 107, OrderId = 5001, Rating = 4, Comment = "Lovely printed cushions, colours as pictured.", CreatedAt = SeedOrderPlacedAt1, IsDemoData = true });
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
            // Placed (Paid) order for a registered customer; includes the low-stock
            // Woven Sofa Cover (3-Seater). Subtotal 69.99 + 54.99 = 124.98, 10% off.
            new Order { Id = 5001, OrderNumber = "ORD-20240210-A1B2C3", CustomerId = 1, GuestEmail = null, GuestName = null, Status = OrderStatus.Paid, Subtotal = 124.98m, DiscountTotal = 12.50m, Tax = 0m, ShippingTotal = 0m, Total = 112.48m, PaymentMethod = PaymentMethod.Card, PaymentStatus = PaymentStatus.Paid, CurrencyCode = "USD", AddressLine1 = "100 Market Street", AddressLine2 = "Apt 4B", City = "Seattle", StateRegion = "WA", PostalCode = "98101", Country = "United States", ContactPhone = "+1-555-0100", PlacedAt = SeedOrderPlacedAt1, IsDemoData = true },
            // Guest order, delivered. Area Rug 160x230 + shipping.
            new Order { Id = 5002, OrderNumber = "ORD-20240215-D4E5F6", CustomerId = null, GuestEmail = "guest@demo.local", GuestName = "Guest Shopper", Status = OrderStatus.Delivered, Subtotal = 159.99m, DiscountTotal = 0m, Tax = 0m, ShippingTotal = 7.50m, Total = 167.49m, PaymentMethod = PaymentMethod.Card, PaymentStatus = PaymentStatus.Paid, CurrencyCode = "USD", AddressLine1 = "22 Elm Avenue", AddressLine2 = null, City = "Portland", StateRegion = "OR", PostalCode = "97201", Country = "United States", ContactPhone = "+1-555-0150", PlacedAt = SeedOrderPlacedAt2, IsDemoData = true },
            // Pending order for the second customer. Macramé Wall Hanging + shipping.
            new Order { Id = 5003, OrderNumber = "ORD-20240301-G7H8J9", CustomerId = 2, GuestEmail = null, GuestName = null, Status = OrderStatus.Pending, Subtotal = 49.99m, DiscountTotal = 0m, Tax = 0m, ShippingTotal = 5.00m, Total = 54.99m, PaymentMethod = PaymentMethod.Cash, PaymentStatus = PaymentStatus.CashOnDelivery, CurrencyCode = "USD", AddressLine1 = "7 Rose Court", AddressLine2 = null, City = "Austin", StateRegion = "TX", PostalCode = "73301", Country = "United States", ContactPhone = "+1-555-0101", PlacedAt = SeedOrderPlacedAt3, IsDemoData = true });
    }

    // Order items: IDs in the 6000-series. FK scalars only.
    private static void SeedOrderItems(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>().HasData(
            // Order 5001: Woven Sofa Cover (3-Seater, low-stock variant) x1 + Printed Cushion Set x1
            new OrderItem { Id = 6001, OrderId = 5001, ProductId = 101, VariantId = 1002, ProductNameSnapshot = "Woven Sofa Cover (Size: 3-Seater)", UnitPrice = 69.99m, Quantity = 1, LineTotal = 69.99m, IsDemoData = true },
            new OrderItem { Id = 6002, OrderId = 5001, ProductId = 107, VariantId = 1008, ProductNameSnapshot = "Printed Cushion Set (Pattern: Floral)", UnitPrice = 54.99m, Quantity = 1, LineTotal = 54.99m, IsDemoData = true },
            // Order 5002: Textured Area Rug (160x230) x1
            new OrderItem { Id = 6003, OrderId = 5002, ProductId = 111, VariantId = 1011, ProductNameSnapshot = "Textured Area Rug (Size: 160x230)", UnitPrice = 159.99m, Quantity = 1, LineTotal = 159.99m, IsDemoData = true },
            // Order 5003: Macramé Wall Hanging x1
            new OrderItem { Id = 6004, OrderId = 5003, ProductId = 109, VariantId = null, ProductNameSnapshot = "Macramé Wall Hanging", UnitPrice = 49.99m, Quantity = 1, LineTotal = 49.99m, IsDemoData = true });
    }
}
