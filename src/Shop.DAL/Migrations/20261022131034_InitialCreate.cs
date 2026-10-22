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
                    IsDemoData = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
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
                columns: new[] { "Id", "Description", "IsDemoData", "Name", "Slug" },
                values: new object[,]
                {
                    { 1, "Clothing and wearables.", true, "Apparel", "apparel" },
                    { 2, "Shoes and sandals.", true, "Footwear", "footwear" },
                    { 3, "Bags, belts, and more.", true, "Accessories", "accessories" },
                    { 4, "Gadgets and devices.", true, "Electronics", "electronics" }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Email", "FullName", "IsDemoData", "MarketingConsent", "Phone" },
                values: new object[,]
                {
                    { 1, "jordan@demo.local", "Jordan Rivera", true, true, "+1-555-0100" },
                    { 2, "sam@demo.local", "Sam Taylor", true, false, "+1-555-0101" }
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
                values: new object[] { 5002, null, 0m, "guest@demo.local", "Guest Shopper", true, "ORD-20240215-D4E5F6", new DateTime(2024, 2, 15, 14, 0, 0, 0, DateTimeKind.Utc), 7.50m, 4, 89.99m, 0m, 97.49m });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { 1, "Admin" });

            migrationBuilder.InsertData(
                table: "AdminUsers",
                columns: new[] { "Id", "Email", "IsActive", "IsDemoData", "PasswordHash", "RoleId", "Username" },
                values: new object[] { 1, "admin@onlineshopping.demo", true, true, "AQAAAAIAAYagAAAAEEFXRDAJVQm+ewFvN7Vr34QeNosx0FUZdw1WUhmkykFNRkcH5bjLM/2rbTxsBQ5hFw==", 1, "admin" });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "CustomerId", "DiscountTotal", "GuestEmail", "GuestName", "IsDemoData", "OrderNumber", "PlacedAt", "ShippingTotal", "Status", "Subtotal", "Tax", "Total" },
                values: new object[,]
                {
                    { 5001, 1, 6.50m, null, null, true, "ORD-20240210-A1B2C3", new DateTime(2024, 2, 10, 9, 30, 0, 0, DateTimeKind.Utc), 0m, 1, 64.98m, 0m, 58.48m },
                    { 5003, 2, 0m, null, null, true, "ORD-20240301-G7H8J9", new DateTime(2024, 3, 1, 11, 15, 0, 0, DateTimeKind.Utc), 5.00m, 0, 39.99m, 0m, 44.99m }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "BasePrice", "Brand", "CategoryId", "CreatedAt", "DescriptionHtml", "IsActive", "IsDemoData", "Name", "Slug" },
                values: new object[,]
                {
                    { 101, 19.99m, "Everyday", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Soft cotton T-shirt.</p>", true, true, "Classic Tee", "classic-tee" },
                    { 102, 44.99m, "Everyday", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Cozy fleece hoodie.</p>", true, true, "Hooded Sweatshirt", "hooded-sweatshirt" },
                    { 103, 79.99m, "BlueLine", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Rugged denim jacket.</p>", true, true, "Denim Jacket", "denim-jacket" },
                    { 104, 89.99m, "Strideo", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Lightweight running shoes.</p>", true, true, "Running Shoes", "running-shoes" },
                    { 105, 129.99m, "TrailMark", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Durable leather boots.</p>", true, true, "Leather Boots", "leather-boots" },
                    { 106, 49.99m, "Strideo", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Casual canvas sneakers.</p>", true, true, "Canvas Sneakers", "canvas-sneakers" },
                    { 107, 34.99m, "TrailMark", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Full-grain leather belt.</p>", true, true, "Leather Belt", "leather-belt" },
                    { 108, 24.99m, "Everyday", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Spacious canvas tote.</p>", true, true, "Tote Bag", "tote-bag" },
                    { 109, 29.99m, "BlueLine", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Warm wool scarf.</p>", true, true, "Wool Scarf", "wool-scarf" },
                    { 110, 59.99m, "SoundCore", 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Noise-isolating earbuds.</p>", true, true, "Wireless Earbuds", "wireless-earbuds" },
                    { 111, 149.99m, "SoundCore", 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>Fitness-tracking smart watch.</p>", true, true, "Smart Watch", "smart-watch" },
                    { 112, 39.99m, "SoundCore", 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "<p>10,000mAh power bank.</p>", true, true, "Portable Charger", "portable-charger" }
                });

            migrationBuilder.InsertData(
                table: "InventoryItems",
                columns: new[] { "Id", "IsDemoData", "LowStockThreshold", "ProductId", "QuantityOnHand", "VariantId" },
                values: new object[,]
                {
                    { 3006, true, 4, 103, 15, null },
                    { 3012, true, 6, 106, 60, null },
                    { 3013, true, 5, 107, 45, null },
                    { 3014, true, 6, 108, 70, null },
                    { 3015, true, 5, 109, 4, null },
                    { 3018, true, 4, 111, 18, null },
                    { 3019, true, 6, 112, 50, null }
                });

            migrationBuilder.InsertData(
                table: "OrderItems",
                columns: new[] { "Id", "IsDemoData", "LineTotal", "OrderId", "ProductId", "ProductNameSnapshot", "Quantity", "UnitPrice", "VariantId" },
                values: new object[,]
                {
                    { 6002, true, 24.99m, 5001, 108, "Tote Bag", 1, 24.99m, null },
                    { 6004, true, 39.99m, 5003, 112, "Portable Charger", 1, 39.99m, null }
                });

            migrationBuilder.InsertData(
                table: "ProductImages",
                columns: new[] { "Id", "AltText", "IsDemoData", "ProductId", "SortOrder", "Url" },
                values: new object[,]
                {
                    { 2001, "Classic Tee front", true, 101, 0, "https://demo.local/img/classic-tee-1.jpg" },
                    { 2002, "Classic Tee back", true, 101, 1, "https://demo.local/img/classic-tee-2.jpg" },
                    { 2003, "Hooded Sweatshirt", true, 102, 0, "https://demo.local/img/hoodie-1.jpg" },
                    { 2004, "Denim Jacket", true, 103, 0, "https://demo.local/img/denim-jacket-1.jpg" },
                    { 2005, "Running Shoes", true, 104, 0, "https://demo.local/img/running-shoes-1.jpg" },
                    { 2006, "Leather Boots", true, 105, 0, "https://demo.local/img/leather-boots-1.jpg" },
                    { 2007, "Canvas Sneakers", true, 106, 0, "https://demo.local/img/canvas-sneakers-1.jpg" },
                    { 2008, "Leather Belt", true, 107, 0, "https://demo.local/img/leather-belt-1.jpg" },
                    { 2009, "Tote Bag", true, 108, 0, "https://demo.local/img/tote-bag-1.jpg" },
                    { 2010, "Wool Scarf", true, 109, 0, "https://demo.local/img/wool-scarf-1.jpg" },
                    { 2011, "Wireless Earbuds", true, 110, 0, "https://demo.local/img/earbuds-1.jpg" },
                    { 2012, "Smart Watch", true, 111, 0, "https://demo.local/img/smart-watch-1.jpg" },
                    { 2013, "Portable Charger", true, 112, 0, "https://demo.local/img/charger-1.jpg" }
                });

            migrationBuilder.InsertData(
                table: "ProductVariants",
                columns: new[] { "Id", "CompareAtPrice", "IsDemoData", "OptionName", "OptionValue", "Price", "ProductId", "Sku" },
                values: new object[,]
                {
                    { 1001, null, true, "Size", "S", 19.99m, 101, "TEE-S" },
                    { 1002, null, true, "Size", "M", 19.99m, 101, "TEE-M" },
                    { 1003, null, true, "Size", "L", 19.99m, 101, "TEE-L" },
                    { 1004, 54.99m, true, "Size", "M", 44.99m, 102, "HOOD-M" },
                    { 1005, 54.99m, true, "Size", "L", 44.99m, 102, "HOOD-L" },
                    { 1006, null, true, "Size", "9", 89.99m, 104, "RUN-9" },
                    { 1007, null, true, "Size", "10", 89.99m, 104, "RUN-10" },
                    { 1008, null, true, "Size", "11", 89.99m, 104, "RUN-11" },
                    { 1009, null, true, "Size", "9", 129.99m, 105, "BOOT-9" },
                    { 1010, null, true, "Size", "10", 129.99m, 105, "BOOT-10" },
                    { 1011, null, true, "Color", "Black", 59.99m, 110, "EAR-BLK" },
                    { 1012, null, true, "Color", "White", 59.99m, 110, "EAR-WHT" }
                });

            migrationBuilder.InsertData(
                table: "InventoryItems",
                columns: new[] { "Id", "IsDemoData", "LowStockThreshold", "ProductId", "QuantityOnHand", "VariantId" },
                values: new object[,]
                {
                    { 3001, true, 5, 101, 50, 1001 },
                    { 3002, true, 5, 101, 3, 1002 },
                    { 3003, true, 5, 101, 40, 1003 },
                    { 3004, true, 5, 102, 20, 1004 },
                    { 3005, true, 5, 102, 2, 1005 },
                    { 3007, true, 5, 104, 25, 1006 },
                    { 3008, true, 5, 104, 30, 1007 },
                    { 3009, true, 5, 104, 5, 1008 },
                    { 3010, true, 3, 105, 12, 1009 },
                    { 3011, true, 3, 105, 8, 1010 },
                    { 3016, true, 5, 110, 35, 1011 },
                    { 3017, true, 5, 110, 22, 1012 }
                });

            migrationBuilder.InsertData(
                table: "OrderItems",
                columns: new[] { "Id", "IsDemoData", "LineTotal", "OrderId", "ProductId", "ProductNameSnapshot", "Quantity", "UnitPrice", "VariantId" },
                values: new object[,]
                {
                    { 6001, true, 39.98m, 5001, 101, "Classic Tee", 2, 19.99m, 1002 },
                    { 6003, true, 89.99m, 5002, 104, "Running Shoes", 1, 89.99m, 1008 }
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
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
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
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
