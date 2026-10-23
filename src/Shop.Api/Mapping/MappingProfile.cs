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
                s.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()))
            // Admin-list stock: units from the product-level inventory row(s).
            .ForMember(d => d.QuantityOnHand, o => o.MapFrom(s =>
                s.InventoryItems.Where(i => i.VariantId == null).Sum(i => i.QuantityOnHand)));

        // Product -> detail: carry variants + images and sum inventory on hand.
        CreateMap<Product, ProductDetailVM>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.TotalQuantityOnHand, o => o.MapFrom(s =>
                s.InventoryItems.Sum(i => i.QuantityOnHand)))
            // Admin-list stock: units from the product-level inventory row(s).
            .ForMember(d => d.QuantityOnHand, o => o.MapFrom(s =>
                s.InventoryItems.Where(i => i.VariantId == null).Sum(i => i.QuantityOnHand)));

        CreateMap<ProductVariant, ProductVariantVM>();
        CreateMap<ProductImage, ProductImageVM>();
        CreateMap<Category, CategoryVM>();

        // ---- Settings read map ----
        CreateMap<StoreSettings, StoreSettingsVM>();

        // ---- Order read maps ----

        // Order -> admin list row: show the customer name when present,
        // otherwise the guest name; render the status enum as its string name.
        CreateMap<Order, AdminOrderListItemVM>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s =>
                s.Customer != null ? s.Customer.FullName : s.GuestName))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.PaymentMethod, o => o.MapFrom(s => s.PaymentMethod.ToString()))
            .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus.ToString()));
        // CurrencyCode, GuestEmail and the address fields map by name.

        // Order -> tracking view: string status + line items.
        CreateMap<Order, OrderTrackingVM>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
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

        // ---- Inventory read map ----

        // InventoryItem -> low-stock row: compose the optional variant label
        // from the variant's option name/value via a resolver expression.
        CreateMap<InventoryItem, LowStockItemVM>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty))
            .ForMember(d => d.VariantLabel, o => o.MapFrom(s =>
                s.Variant != null ? s.Variant.OptionName + ": " + s.Variant.OptionValue : null));

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
            .ForMember(d => d.Images, o => o.Ignore())
            .ForMember(d => d.InventoryItems, o => o.Ignore());

        // DiscountUpsertRequest -> Discount: map Code/Type/Value/IsActive/
        // StartsAt/EndsAt; Id and IsDemoData are server-owned.
        CreateMap<DiscountUpsertRequest, Discount>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsDemoData, o => o.Ignore());
    }
}
