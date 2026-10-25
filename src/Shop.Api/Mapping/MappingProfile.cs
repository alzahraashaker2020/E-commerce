using AutoMapper;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;

namespace Shop.Api.Mapping;

/// <summary>
/// The single AutoMapper profile for the API. It defines the read maps
/// (entity to <c>*VM</c>) and the two write maps (<c>*Request</c> to entity).
/// Each write map explicitly ignores the server-owned destination members so
/// <c>AssertConfigurationIsValid()</c> passes. Grouped by domain area:
/// catalog, orders, discounts, customers, inventory.
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// Configures every entity-to-ViewModel read map and the two
    /// request-to-entity write maps used by the API.
    /// </summary>
    public MappingProfile()
    {
        // ---- Catalog read maps ----

        // Product -> list row: flatten the category name and pick the first
        // image (by sort order) as the primary thumbnail.
        CreateMap<Product, ProductListItemVM>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.PrimaryImageUrl, o => o.MapFrom(s =>
                s.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()
                ?? s.Variants.Where(v => v.IsActive && v.ImageUrl != null && v.ImageUrl != "")
                    .OrderBy(v => v.Id).Select(v => v.ImageUrl).FirstOrDefault()));
        // HasVariants/ShowStockQuantity/StockQuantity/LowStockThreshold map by name
        // from the normalized product columns.

        // Product -> detail: carry variants + images; stock maps from the
        // product-level columns by name (variant stock lives on each variant).
        CreateMap<Product, ProductDetailVM>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.Images, o => o.MapFrom(s => s.Images.OrderBy(i => i.SortOrder)));

        // Variant -> VM: carry the size/color ids and resolved names from the navs.
        CreateMap<ProductVariant, ProductVariantVM>()
            .ForMember(d => d.SizeName, o => o.MapFrom(s => s.Size != null ? s.Size.Name : null))
            .ForMember(d => d.ColorName, o => o.MapFrom(s => s.Color != null ? s.Color.Name : null));
        CreateMap<ProductImage, ProductImageVM>();
        CreateMap<Category, CategoryVM>();

        // ---- Size / Color read maps ----
        CreateMap<Size, SizeVM>();
        CreateMap<Color, ColorVM>();

        // ---- Settings read map ----
        CreateMap<StoreSettings, StoreSettingsVM>();

        // ---- Order read maps ----

        // Order -> admin list row: show the customer name when present,
        // otherwise the guest name; render the status enum as its string name.
        CreateMap<Order, AdminOrderListItemVM>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s =>
                s.Customer != null ? s.Customer.FullName : s.GuestName))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.StatusDefinition != null ? s.StatusDefinition.Name : s.Status.ToString()))
            .ForMember(d => d.StatusId, o => o.MapFrom(s => s.StatusDefinitionId))
            .ForMember(d => d.StatusColor, o => o.MapFrom(s => s.StatusDefinition != null ? s.StatusDefinition.Color : null))
            .ForMember(d => d.PaymentMethod, o => o.MapFrom(s => s.PaymentMethod.ToString()))
            .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus.ToString()));
        // CurrencyCode, GuestEmail and the address fields map by name.

        // Order -> tracking view: string status + line items.
        CreateMap<Order, OrderTrackingVM>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.StatusDefinition != null ? s.StatusDefinition.Name : s.Status.ToString()))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));

        CreateMap<OrderItem, OrderItemVM>();

        // ---- Discount read map ----

        // Discount -> VM: render the discount type enum as its string name.
        CreateMap<Discount, DiscountVM>()
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));

        // ---- Customer read maps ----
        CreateMap<Customer, CustomerListItemVM>();
        CreateMap<Customer, CustomerProfileVM>();

        // ---- Review read map ----
        // Review -> VM: flatten author (customer full name) and product name.
        CreateMap<Review, ReviewVM>()
            .ForMember(d => d.AuthorName, o => o.MapFrom(s => s.Customer != null ? s.Customer.FullName : "Customer"))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty));

        // ---- Write maps (ignore server-owned destination members) ----

        // ProductUpsertRequest -> Product: map the client-editable fields only.
        // Id/Slug/CreatedAt/IsDemoData and every navigation member are owned by
        // the server and must be ignored so the config stays valid.
        CreateMap<ProductUpsertRequest, Product>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Slug, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.IsDemoData, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Variants, o => o.Ignore())
            .ForMember(d => d.Images, o => o.Ignore());

        // DiscountUpsertRequest -> Discount: map Code/Type/Value/IsActive/
        // StartsAt/EndsAt; Id and IsDemoData are server-owned.
        CreateMap<DiscountUpsertRequest, Discount>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsDemoData, o => o.Ignore());
    }
}
