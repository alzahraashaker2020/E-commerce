using AutoMapper;
using Shop.Api.Mapping;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;
using Xunit;
namespace Shop.RegressionTests;

public class MappingContractTests
{
    private readonly MapperConfiguration _configuration = new(c => c.AddProfile<MappingProfile>());
    [Fact] public void Mapping_configuration_is_valid() => _configuration.AssertConfigurationIsValid();
    [Fact]
    public void Product_update_preserves_identity_audit_and_navigation()
    {
        var product = new Product { Id = 42, Slug = "owned-slug", CreatedAt = new DateTime(2020, 1, 1), IsDemoData = true };
        var variants = product.Variants; var images = product.Images;
        _configuration.CreateMapper().Map(new ProductUpsertRequest { Name = "Editable", BasePrice = 12 }, product);
        Assert.Equal(42, product.Id); Assert.Equal("owned-slug", product.Slug);
        Assert.Equal(new DateTime(2020, 1, 1), product.CreatedAt); Assert.True(product.IsDemoData);
        Assert.Same(variants, product.Variants); Assert.Same(images, product.Images); Assert.Equal("Editable", product.Name);
    }
}
