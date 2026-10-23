using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Shop.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartToken = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ParentCategoryId = table.Column<int>(type: "integer", nullable: true),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    MarketingConsent = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    ExternalProvider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsRegistered = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Discounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DescriptionHtml = table.Column<string>(type: "text", nullable: true),
                    Brand = table.Column<string>(type: "text", nullable: true),
                    BasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    GuestEmail = table.Column<string>(type: "text", nullable: true),
                    GuestName = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlacedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminUsers_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AltText = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OptionName = table.Column<string>(type: "text", nullable: false),
                    OptionValue = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CompareAtPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    VariantId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    VariantId = table.Column<int>(type: "integer", nullable: true),
                    QuantityOnHand = table.Column<int>(type: "integer", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "integer", nullable: false),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    VariantId = table.Column<int>(type: "integer", nullable: true),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "ImageUrl", "IsDemoData", "Name", "ParentCategoryId", "Slug" },
                values: new object[,]
                {
                    { 1, "Covers, throws, and cushions for your sofa.", "assets/images/categories/sofa.webp", true, "Sofa Styling", null, "sofa-styling" },
                    { 2, "Macramé, hangings, and wall décor.", "assets/images/categories/wall.webp", true, "Wall Styling", null, "wall-styling" },
                    { 3, "Rugs, runners, and floor accents.", "assets/images/categories/floor.webp", true, "Floor Styling", null, "floor-styling" },
                    { 4, "The latest additions to the collection.", "assets/images/categories/newarrivals.webp", true, "New Arrivals", null, "new-arrivals" }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "CreatedAt", "Email", "ExternalId", "ExternalProvider", "FullName", "IsDemoData", "IsRegistered", "MarketingConsent", "PasswordHash", "Phone" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "jordan@demo.local", null, null, "Jordan Rivera", true, true, true, "AQAAAAIAAYagAAAAEEFXRDAJVQm+ewFvN7Vr34QeNosx0FUZdw1WUhmkykFNRkcH5bjLM/2rbTxsBQ5hFw==", "+1-555-0100" },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "sam@demo.local", null, null, "Sam Taylor", true, false, false, null, "+1-555-0101" }
                });

            migrationBuilder.InsertData(
                table: "Discounts",
                columns: new[] { "Id", "Code", "EndsAt", "IsActive", "IsDemoData", "StartsAt", "Type", "Value" },
                values: new object[,]
                {
                    { 1, "WELCOME10", null, true, true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, 10m },
                    { 2, "SAVE5", null, true, true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 5m }
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "CustomerId", "DiscountTotal", "GuestEmail", "GuestName", "IsDemoData", "OrderNumber", "PlacedAt", "ShippingTotal", "Status", "Subtotal", "Tax", "Total" },
                values: new object[] { 5002, null, 0m, "guest@demo.local", "Guest Shopper", true, "ORD-20240215-D4E5F6", new DateTime(2024, 2, 15, 14, 0, 0, 0, DateTimeKind.Utc), 7.50m, 4, 159.99m, 0m, 167.49m });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { 1, "Admin" });

            migrationBuilder.InsertData(
                table: "AdminUsers",
                columns: new[] { "Id", "Email", "IsActive", "IsDemoData", "PasswordHash", "RoleId", "Username" },
                values: new object[] { 1, "admin@onlineshopping.demo", true, true, "AQAAAAIAAYagAAAAEEFXRDAJVQm+ewFvN7Vr34QeNosx0FUZdw1WUhmkykFNRkcH5bjLM/2rbTxsBQ5hFw==", 1, "admin" });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "ImageUrl", "IsDemoData", "Name", "ParentCategoryId", "Slug" },
                values: new object[,]
                {
                    { 11, "Protective, elegant sofa covers.", "assets/images/sofa/sofa-covers.webp", true, "Sofa covers", 1, "sofa-covers" },
                    { 12, "Soft throws and blankets.", "assets/images/sofa/blanket-throw.webp", true, "Blanket / throw out", 1, "blanket-throw" },
                    { 13, "Handcrafted cushion and cover sets.", "assets/images/sofa/handmade-sets.webp", true, "Hand made sets", 1, "hand-made-sets" },
                    { 14, "Patterned, printed cushion sets.", "assets/images/sofa/printed-sets.webp", true, "Printed sets", 1, "printed-sets" },
                    { 21, "Woven macramé wall hangings.", "assets/images/categories/wall.webp", true, "Macramé hangings", 2, "macrame-hangings" },
                    { 22, "Decorative wall frames.", "assets/images/categories/wall.webp", true, "Wall frames", 2, "wall-frames" },
                    { 31, "Soft area rugs.", "assets/images/categories/floor.webp", true, "Area rugs", 3, "area-rugs" },
                    { 32, "Hallway and kitchen runners.", "assets/images/categories/floor.webp", true, "Runners", 3, "runners" },
                    { 41, "Freshly added items.", "assets/images/categories/newarrivals.webp", true, "Latest picks", 4, "latest-picks" }
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "CustomerId", "DiscountTotal", "GuestEmail", "GuestName", "IsDemoData", "OrderNumber", "PlacedAt", "ShippingTotal", "Status", "Subtotal", "Tax", "Total" },
                values: new object[,]
                {
                    { 5001, 1, 12.50m, null, null, true, "ORD-20240210-A1B2C3", new DateTime(2024, 2, 10, 9, 30, 0, 0, DateTimeKind.Utc), 0m, 1, 124.98m, 0m, 112.48m },
                    { 5003, 2, 0m, null, null, true, "ORD-20240301-G7H8J9", new DateTime(2024, 3, 1, 11, 15, 0, 0, DateTimeKind.Utc), 5.00m, 0, 49.99m, 0m, 54.99m }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "BasePrice", "Brand", "CategoryId", "CreatedAt", "DescriptionHtml", "IsActive", "IsDemoData", "Name", "Slug" },
                values: new object[,]
                {
                    { 101, 59.99m, "Makani", 11, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Soft woven cover that drapes any sofa in elegance.</p>", true, true, "Woven Sofa Cover", "woven-sofa-cover" },
                    { 102, 74.99m, "Makani", 11, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Form-fitting stretch slipcover for a tailored look.</p>", true, true, "Stretch Slipcover", "stretch-slipcover" },
                    { 103, 39.99m, "Makani", 12, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Cozy tasseled throw in warm neutral tones.</p>", true, true, "Tasseled Throw Blanket", "tasseled-throw-blanket" },
                    { 104, 44.99m, "Makani", 12, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Chunky knitted throw for the sofa or bed.</p>", true, true, "Knitted Throw", "knitted-throw" },
                    { 105, 69.99m, "Makani", 13, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Set of handcrafted textured cushions.</p>", true, true, "Handmade Cushion Set", "handmade-cushion-set" },
                    { 106, 84.99m, "Makani", 13, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Three artisan pillows in complementary weaves.</p>", true, true, "Artisan Pillow Trio", "artisan-pillow-trio" },
                    { 107, 54.99m, "Makani", 14, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Patterned printed cushion set to accent any sofa.</p>", true, true, "Printed Cushion Set", "printed-cushion-set" },
                    { 108, 42.99m, "Makani", 14, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Pair of floral-print pillows.</p>", true, true, "Floral Print Pillows", "floral-print-pillows" },
                    { 109, 49.99m, "Makani", 21, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Hand-knotted macramé wall hanging.</p>", true, true, "Macramé Wall Hanging", "macrame-wall-hanging" },
                    { 110, 34.99m, "Makani", 22, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Decorative boho wall frame.</p>", true, true, "Boho Wall Frame", "boho-wall-frame" },
                    { 111, 119.99m, "Makani", 31, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Soft textured area rug in neutral tones.</p>", true, true, "Textured Area Rug", "textured-area-rug" },
                    { 112, 64.99m, "Makani", 32, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Durable woven runner for hallways.</p>", true, true, "Woven Floor Runner", "woven-floor-runner" },
                    { 113, 47.99m, "Makani", 41, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>New-season throw in autumn hues.</p>", true, true, "Autumn Collection Throw", "autumn-collection-throw" }
                });

            migrationBuilder.InsertData(
                table: "InventoryItems",
                columns: new[] { "Id", "IsDemoData", "LowStockThreshold", "ProductId", "QuantityOnHand", "VariantId" },
                values: new object[,]
                {
                    { 3006, true, 6, 103, 55, null },
                    { 3007, true, 5, 104, 30, null },
                    { 3010, true, 4, 106, 16, null },
                    { 3013, true, 5, 108, 38, null },
                    { 3014, true, 4, 109, 12, null },
                    { 3015, true, 5, 110, 4, null },
                    { 3019, true, 5, 112, 28, null },
                    { 3020, true, 5, 113, 33, null }
                });

            migrationBuilder.InsertData(
                table: "OrderItems",
                columns: new[] { "Id", "IsDemoData", "LineTotal", "OrderId", "ProductId", "ProductNameSnapshot", "Quantity", "UnitPrice", "VariantId" },
                values: new object[] { 6004, true, 49.99m, 5003, 109, "Macramé Wall Hanging", 1, 49.99m, null });

            migrationBuilder.InsertData(
                table: "ProductImages",
                columns: new[] { "Id", "AltText", "IsDemoData", "ProductId", "SortOrder", "Url" },
                values: new object[,]
                {
                    { 2001, "Woven Sofa Cover", true, 101, 0, "assets/images/sofa/sofa-covers.webp" },
                    { 2002, "Stretch Slipcover", true, 102, 0, "assets/images/sofa/sofa-covers.webp" },
                    { 2003, "Tasseled Throw Blanket", true, 103, 0, "assets/images/sofa/blanket-throw.webp" },
                    { 2004, "Knitted Throw", true, 104, 0, "assets/images/sofa/blanket-throw.webp" },
                    { 2005, "Handmade Cushion Set", true, 105, 0, "assets/images/sofa/handmade-sets.webp" },
                    { 2006, "Artisan Pillow Trio", true, 106, 0, "assets/images/sofa/handmade-sets.webp" },
                    { 2007, "Printed Cushion Set", true, 107, 0, "assets/images/sofa/printed-sets.webp" },
                    { 2008, "Floral Print Pillows", true, 108, 0, "assets/images/sofa/printed-sets.webp" },
                    { 2009, "Macramé Wall Hanging", true, 109, 0, "assets/images/categories/wall.webp" },
                    { 2010, "Boho Wall Frame", true, 110, 0, "assets/images/categories/wall.webp" },
                    { 2011, "Textured Area Rug", true, 111, 0, "assets/images/categories/floor.webp" },
                    { 2012, "Woven Floor Runner", true, 112, 0, "assets/images/categories/floor.webp" },
                    { 2013, "Autumn Collection Throw", true, 113, 0, "assets/images/categories/newarrivals.webp" }
                });

            migrationBuilder.InsertData(
                table: "ProductVariants",
                columns: new[] { "Id", "CompareAtPrice", "IsDemoData", "OptionName", "OptionValue", "Price", "ProductId", "Sku" },
                values: new object[,]
                {
                    { 1001, null, true, "Size", "2-Seater", 59.99m, 101, "COVER-2S" },
                    { 1002, null, true, "Size", "3-Seater", 69.99m, 101, "COVER-3S" },
                    { 1003, null, true, "Size", "L-Shape", 89.99m, 101, "COVER-LS" },
                    { 1004, 84.99m, true, "Size", "2-Seater", 74.99m, 102, "SLIP-2S" },
                    { 1005, 94.99m, true, "Size", "3-Seater", 84.99m, 102, "SLIP-3S" },
                    { 1006, null, true, "Colour", "Beige", 69.99m, 105, "CUSH-BEIGE" },
                    { 1007, null, true, "Colour", "Clay", 69.99m, 105, "CUSH-CLAY" },
                    { 1008, null, true, "Pattern", "Floral", 54.99m, 107, "PRNT-FLORAL" },
                    { 1009, null, true, "Pattern", "Geometric", 54.99m, 107, "PRNT-GEO" },
                    { 1010, null, true, "Size", "120x170", 119.99m, 111, "RUG-120" },
                    { 1011, null, true, "Size", "160x230", 159.99m, 111, "RUG-160" },
                    { 1012, null, true, "Size", "200x290", 199.99m, 111, "RUG-200" }
                });

            migrationBuilder.InsertData(
                table: "Reviews",
                columns: new[] { "Id", "Comment", "CreatedAt", "CustomerId", "IsDemoData", "OrderId", "ProductId", "Rating" },
                values: new object[,]
                {
                    { 7001, "The woven cover transformed our sofa — beautiful quality.", new DateTime(2024, 2, 10, 9, 30, 0, 0, DateTimeKind.Utc), 1, true, 5001, 101, 5 },
                    { 7002, "Lovely printed cushions, colours as pictured.", new DateTime(2024, 2, 10, 9, 30, 0, 0, DateTimeKind.Utc), 1, true, 5001, 107, 4 }
                });

            migrationBuilder.InsertData(
                table: "InventoryItems",
                columns: new[] { "Id", "IsDemoData", "LowStockThreshold", "ProductId", "QuantityOnHand", "VariantId" },
                values: new object[,]
                {
                    { 3001, true, 5, 101, 40, 1001 },
                    { 3002, true, 5, 101, 3, 1002 },
                    { 3003, true, 5, 101, 18, 1003 },
                    { 3004, true, 5, 102, 22, 1004 },
                    { 3005, true, 5, 102, 2, 1005 },
                    { 3008, true, 4, 105, 14, 1006 },
                    { 3009, true, 4, 105, 4, 1007 },
                    { 3011, true, 5, 107, 26, 1008 },
                    { 3012, true, 5, 107, 20, 1009 },
                    { 3016, true, 4, 111, 15, 1010 },
                    { 3017, true, 4, 111, 9, 1011 },
                    { 3018, true, 4, 111, 6, 1012 }
                });

            migrationBuilder.InsertData(
                table: "OrderItems",
                columns: new[] { "Id", "IsDemoData", "LineTotal", "OrderId", "ProductId", "ProductNameSnapshot", "Quantity", "UnitPrice", "VariantId" },
                values: new object[,]
                {
                    { 6001, true, 69.99m, 5001, 101, "Woven Sofa Cover (Size: 3-Seater)", 1, 69.99m, 1002 },
                    { 6002, true, 54.99m, 5001, 107, "Printed Cushion Set (Pattern: Floral)", 1, 54.99m, 1008 },
                    { 6003, true, 159.99m, 5002, 111, "Textured Area Rug (Size: 160x230)", 1, 159.99m, 1011 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_RoleId",
                table: "AdminUsers",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Username",
                table: "AdminUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_VariantId",
                table: "CartItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_Code",
                table: "Discounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ProductId",
                table: "InventoryItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_VariantId",
                table: "InventoryItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_VariantId",
                table: "OrderItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_OrderId",
                table: "Reviews",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId",
                table: "Reviews",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "Discounts");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
