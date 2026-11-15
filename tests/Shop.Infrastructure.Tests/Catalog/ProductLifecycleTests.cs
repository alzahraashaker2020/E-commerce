using Microsoft.EntityFrameworkCore;
using Shop.Application.Catalog;
using Shop.Application.Common;
using Shop.Infrastructure.Catalog;
using Shop.DAL.Data;
using Xunit;
namespace Shop.Infrastructure.Tests.Catalog;

public class ProductLifecycleTests
{
    [Fact]
    public async Task Gallery_update_preserves_variant_identity_and_failed_update_is_not_saved()
    {
        var options = new DbContextOptionsBuilder<ShopContext>(); var pg = Environment.GetEnvironmentVariable("SHOP_TEST_POSTGRES");
        if (SqlServerTestDatabase.Configure(options)) { }
        else if (string.IsNullOrWhiteSpace(pg)) options.UseSqlite("Data Source=" + Path.Combine(Path.GetTempPath(), "shop-refactor-" + Guid.NewGuid().ToString("N") + ".db"));
        else options.UseNpgsql(new Npgsql.NpgsqlConnectionStringBuilder(pg) { Database = "shop_refactor_" + Guid.NewGuid().ToString("N") }.ConnectionString);
        int id, variantId; var config = options.Options;
        await using (var db = new ShopContext(config))
        {
            if (db.Database.IsSqlServer()) { await db.Database.EnsureCreatedAsync(); SqlServerSchema.Apply(db, true); } else if (db.Database.IsNpgsql()) await db.Database.MigrateAsync(); else { await db.Database.EnsureCreatedAsync(); SqliteSchemaUpgrade.Apply(db, true); }
            var category = await db.Categories.FirstAsync(); var service = new ProductManagementService(new EfProductCatalogStore(db), new Clock());
            var result = await service.CreateAsync(new() { Name = "Catalog identity", CategoryId = category.Id, IsActive = true, HasVariants = true, Variants = new() { new() { Price = 20, StockQuantity = 5, IsActive = true, ImageUrls = new() { "a", "b" } } }, ImageUrls = new() { "one", "two" } }, null);
            id = result.Id; variantId = await db.ProductVariants.Where(v => v.ProductId == id).Select(v => v.Id).SingleAsync();
        }
        await using (var db = new ShopContext(config))
        {
            var category = await db.Products.Where(p => p.Id == id).Select(p => p.CategoryId).SingleAsync(); var service = new ProductManagementService(new EfProductCatalogStore(db), new Clock());
            var result = await service.UpdateAsync(id, new() { Name = "Updated", CategoryId = category, IsActive = true, HasVariants = true, Variants = new() { new() { Id = variantId, Price = 25, StockQuantity = 6, ImageUrls = new() { "c", "d" } } }, ImageUrls = new() { "three", "four" } });
            Assert.Equal(ProductOutcomeKind.Success, result.Kind);
        }
        await using (var db = new ShopContext(config))
        {
            var variant = await db.ProductVariants.SingleAsync(v => v.ProductId == id); Assert.Equal(variantId, variant.Id); Assert.Equal(6, variant.StockQuantity); Assert.Equal(new[] { "c", "d" }, variant.ImageUrls);
            Assert.Equal(new[] { "three", "four" }, await db.ProductImages.Where(i => i.ProductId == id).OrderBy(i => i.SortOrder).Select(i => i.Url).ToArrayAsync());
            var category = (await db.Products.FindAsync(id))!.CategoryId;
            var result = await new ProductManagementService(new EfProductCatalogStore(db), new Clock()).UpdateAsync(id, new() { Name = "Must not save", CategoryId = category, HasVariants = true, Variants = new() { new() { Id = variantId }, new() { Id = variantId } } });
            Assert.Equal(ProductOutcomeKind.Invalid, result.Kind);
        }
        await using (var db = new ShopContext(config)) Assert.Equal("Updated", (await db.Products.FindAsync(id))!.Name);
    }
    private sealed class Clock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
}
