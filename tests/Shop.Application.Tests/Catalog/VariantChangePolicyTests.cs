using Shop.Application.Catalog;
using Xunit;
namespace Shop.Application.Tests.Catalog;

public class VariantChangePolicyTests
{
    [Fact]
    public void Null_gallery_preserves_and_empty_gallery_clears()
    {
        var p = new CatalogProduct { Name = "Test", Images = new() { new() { Id = 7, Url = "original", SortOrder = 0 } } };
        VariantChangePolicy.SetGallery(p, null); Assert.Equal(7, Assert.Single(p.Images).Id);
        VariantChangePolicy.SetGallery(p, new()); Assert.Empty(p.Images);
    }
    [Fact]
    public void Gallery_order_and_primary_image_are_preserved()
    {
        var p = new CatalogProduct { Name = "Test" }; VariantChangePolicy.SetGallery(p, new() { " second ", "first", "second", " " });
        Assert.Equal(new[] { "second", "first" }, p.Images.Select(i => i.Url)); Assert.Equal(new[] { 0, 1 }, p.Images.Select(i => i.SortOrder));
        VariantChangePolicy.SetPrimaryImage(p, "replacement"); Assert.Equal("replacement", p.Images[0].Url);
        VariantChangePolicy.SetPrimaryImage(p, ""); Assert.Equal("first", Assert.Single(p.Images).Url);
    }
    [Fact]
    public void New_variant_gallery_overrides_legacy_primary_but_null_preserves_it()
    {
        var p = new ProductInput { HasVariants = true, Variants = new() { new() { ImageUrl = "legacy", ImageUrls = null }, new() { ImageUrl = "legacy", ImageUrls = new() { " a ", "b", "a" } } } };
        var rows = VariantChangePolicy.BuildVariants(p); Assert.Equal("legacy", rows[0].ImageUrl); Assert.Empty(rows[0].ImageUrls); Assert.Equal("a", rows[1].ImageUrl); Assert.Equal(new[] { "a", "b" }, rows[1].ImageUrls);
    }
}
