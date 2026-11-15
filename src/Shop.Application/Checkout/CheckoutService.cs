using Shop.Application.Common;
namespace Shop.Application.Checkout;

public sealed class CheckoutService
{
    private readonly ICheckoutStore _store; private readonly ICheckoutBoundary _boundary; private readonly IClock _clock;
    public CheckoutService(ICheckoutStore store, ICheckoutBoundary boundary, IClock clock) { _store = store; _boundary = boundary; _clock = clock; }
    public async Task<CheckoutOutcome> PlaceAsync(CheckoutInput request, CheckoutActor actor, string hash, CancellationToken ct = default)
    {
        if (request.PaymentMethod != 0) return new(CheckoutOutcomeKind.Invalid, Message: "Card payments are unavailable until the store connects a payment provider. Please use cash on delivery.");
        if (!request.IdempotencyKey.HasValue || request.IdempotencyKey == Guid.Empty) return new(CheckoutOutcomeKind.Invalid, Message: "A unique checkout attempt key is required.");
        if (actor.HasAuthorizationHeader && !actor.Authenticated) return new(CheckoutOutcomeKind.Unauthenticated, Message: "Your session expired. Sign in again before placing the order.");
        if (actor.Authenticated && !actor.IsCustomer) return new(CheckoutOutcomeKind.Forbidden, Message: "Use a customer account or guest checkout.");
        CheckoutCustomer? customer = null;
        if (actor.IsCustomer) { if (!actor.CustomerId.HasValue || (customer = await _store.CustomerAsync(actor.CustomerId.Value, ct)) is null) return new(CheckoutOutcomeKind.Unauthenticated, Message: "Customer account is unavailable."); }
        string key = request.IdempotencyKey.Value.ToString("N"), owner = customer is null ? "guest" : "customer:" + customer.Id;
        await using var session = await _store.OpenAsync(ct);
        async Task<CheckoutOutcome?> Replay()
        {
            var receipt = await session.ReceiptAsync(key, ct); if (receipt is null) return null;
            if (receipt.Owner != owner || receipt.Hash != hash) return new(CheckoutOutcomeKind.Conflict, Message: "This checkout attempt was already used with different details. Start a new checkout attempt.");
            return new(CheckoutOutcomeKind.Success, ReplayJson: receipt.Json);
        }
        var replay = await Replay(); if (replay is not null) return replay;
        await session.BeginAsync(ct); replay = await Replay(); if (replay is not null) return replay;
        try
        {
            var result = await PlaceCoreAsync(request, customer, ct);
            if (result.Kind != CheckoutOutcomeKind.Success) return result with { NotCreated = true };
            await session.WriteReceiptAsync(key, owner, hash, _boundary.EncodeReceipt(result.Confirmation!, result.Message), ct);
            await _store.QueueSummaryAsync(result.Confirmation!.OrderNumber, ct);
            await session.CommitAsync(ct); return result;
        }
        catch (CheckoutDuplicateException)
        {
            await session.RollbackAsync(ct);
            return await Replay() ?? new(CheckoutOutcomeKind.Conflict, Message: "Checkout is being processed. Retry the same submission.");
        }
    }
    private async Task<CheckoutOutcome> PlaceCoreAsync(CheckoutInput request, CheckoutCustomer? customer, CancellationToken ct)
    {
        if (request.Lines is null || request.Lines.Count == 0)
        {
            return new(CheckoutOutcomeKind.Invalid, Message: "Cart is empty/invalid");
        }

        // Resolve each line against the live product/variant, building snapshot
        // order items with server-trusted prices.
        var items = new List<PricedLine>();
        foreach (CheckoutLine line in request.Lines)
        {
            CheckoutProduct? product = await _store.ProductAsync(line.ProductId, false, ct);
            if (product is null)
            {
                return new(CheckoutOutcomeKind.Invalid, Message:
                    $"Product {line.ProductId} not found");
            }

            if (!product.IsActive || (product.HasVariants && !line.VariantId.HasValue))
                return new(CheckoutOutcomeKind.Invalid, Message: "Select an available product and its size/color variant.");

            long requestedQuantity = request.Lines
                .Where(l => l.ProductId == line.ProductId && l.VariantId == line.VariantId)
                .Sum(l => (long)l.Quantity);
            if (!line.VariantId.HasValue && requestedQuantity > product.StockQuantity)
                return new(CheckoutOutcomeKind.Conflict, Message: "The requested product quantity exceeds available stock.");

            decimal unitPrice = product.BasePrice;
            string nameSnapshot = product.Name;
            string? sizeSnapshot = null;
            string? colorSnapshot = null;

            if (line.VariantId.HasValue)
            {
                CheckoutVariant? variant = product.Variants
                    .FirstOrDefault(v => v.Id == line.VariantId.Value);
                if (variant is null)
                {
                    return new(CheckoutOutcomeKind.Invalid, Message:
                        $"Variant {line.VariantId.Value} not found for product {line.ProductId}");
                }

                if (!variant.IsActive || requestedQuantity > variant.StockQuantity)
                    return new(CheckoutOutcomeKind.Conflict, Message: "The selected size/color has insufficient stock or is unavailable.");

                unitPrice = variant.Price;
                sizeSnapshot = variant.Size?.Name;
                colorSnapshot = variant.Color?.Name;

                var parts = new List<string>();
                if (sizeSnapshot is not null)
                {
                    parts.Add(sizeSnapshot);
                }

                if (colorSnapshot is not null)
                {
                    parts.Add(colorSnapshot);
                }

                nameSnapshot = parts.Count == 0
                    ? product.Name
                    : $"{product.Name} ({string.Join(", ", parts)})";
            }

            items.Add(new PricedLine
            {
                ProductId = product.Id,
                VariantId = line.VariantId,
                ProductNameSnapshot = nameSnapshot,
                SizeSnapshot = sizeSnapshot,
                ColorSnapshot = colorSnapshot,
                UnitPrice = unitPrice,
                Quantity = line.Quantity,
                LineTotal = unitPrice * line.Quantity,
                IsDemoData = false
            });
        }

        decimal subtotal = items.Sum(i => i.LineTotal);

        // Apply the discount code when present and currently valid; otherwise
        // the order is placed WITHOUT a discount (never a 400) and the response
        // explains why.
        decimal discountTotal = 0m;
        string? message = null;
        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            (discountTotal, message) = await ResolveDiscountAsync(request.DiscountCode, subtotal, ct);
        }

        decimal total = subtotal - discountTotal;
        if (total < 0m)
        {
            total = 0m;
        }

        // Load the store settings for the order currency (fall back to USD/$ if
        // the single settings row is somehow missing).
        CheckoutSettings settings = await _store.SettingsAsync(false, ct) ?? new("USD", "$");
        var area = await _store.AreaAsync(request.AreaId, settings.CurrencyCode, ct);
        if (area is null) return new(CheckoutOutcomeKind.Invalid, Message: "Delivery is unavailable for this area or store currency. Please select another area.");
        total += area.ShippingFee;
        if ((request.ExpectedTotal.HasValue && request.ExpectedTotal.Value != total) ||
            (request.ExpectedCurrencyCode is not null && request.ExpectedCurrencyCode != settings.CurrencyCode))
            return new(CheckoutOutcomeKind.Conflict, Message: "The total changed. Review the updated shipping and order total before placing the order again.");

        var now = _clock.UtcNow;
        var initialStatus = await _store.InitialStatusAsync(ct);
        var order = new CheckoutOrder
        {
            CustomerId = customer?.Id,
            GuestName = customer?.FullName ?? request.GuestName,
            GuestEmail = customer?.Email ?? request.GuestEmail,
            Status = "Pending",
            StatusDefinitionId = initialStatus.Id,
            StatusDefinition = initialStatus,
            Subtotal = subtotal,
            DiscountTotal = discountTotal,
            Tax = 0m,
            ShippingTotal = area.ShippingFee,
            DeliveryAreaId = area.Id,
            Total = total,
            CurrencyCode = settings.CurrencyCode,
            PaymentMethod = "Cash",
            PaymentStatus = "CashOnDelivery",
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = area.Name,
            StateRegion = area.Governorate!.Name,
            PostalCode = request.PostalCode,
            Country = request.Country,
            ContactPhone = request.ContactPhone,
            PlacedAt = now,
            IsDemoData = false,
            Items = items
        };

        order.OrderNumber = _boundary.NewOrderNumber(now);
        await _store.SaveOrderAsync(order, ct);

        var confirmation = new CheckoutConfirmation
        {
            TrackingToken = _boundary.CreateTrackingProof(order.OrderNumber),
            OrderNumber = order.OrderNumber,
            Subtotal = order.Subtotal,
            DiscountTotal = order.DiscountTotal,
            ShippingTotal = order.ShippingTotal,
            Total = order.Total,
            Status = initialStatus.Name,
            PlacedAt = order.PlacedAt,
            CurrencyCode = order.CurrencyCode,
            PaymentMethod = order.PaymentMethod.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            AddressLine1 = order.AddressLine1,
            AddressLine2 = order.AddressLine2,
            City = order.City,
            StateRegion = order.StateRegion,
            PostalCode = order.PostalCode,
            Country = order.Country,
            ContactPhone = order.ContactPhone
        };

        return new(CheckoutOutcomeKind.Success, confirmation, message);
    }
    public async Task<QuoteOutcome> QuoteAsync(QuoteInput request, CancellationToken ct = default)
    {
        var currency = (await _store.SettingsAsync(true, ct))!.CurrencyCode;
        var area = await _store.AreaAsync(request.AreaId, currency, ct);
        if (area is null) return new(CheckoutOutcomeKind.Invalid, Message: "Delivery is unavailable for the selected area.");
        decimal subtotal = 0;
        foreach (var line in request.Lines.GroupBy(l => new { l.ProductId, l.VariantId }))
        {
            var product = await _store.ProductAsync(line.Key.ProductId, true, ct);
            if (product is null) return new(CheckoutOutcomeKind.Invalid, Message: "Product is unavailable.");
            long quantity = line.Sum(l => (long)l.Quantity);
            decimal price = product.BasePrice;
            if (line.Key.VariantId.HasValue)
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == line.Key.VariantId && v.IsActive);
                if (variant is null || variant.StockQuantity < quantity) return new(CheckoutOutcomeKind.Conflict, Message: "Selected size/color has insufficient stock.");
                price = variant.Price;
            }
            else if (product.HasVariants || product.StockQuantity < quantity)
                return new(CheckoutOutcomeKind.Conflict, Message: "Select an available product size/color and quantity.");
            subtotal += price * quantity;
        }
        var discount = string.IsNullOrWhiteSpace(request.DiscountCode) ? (0m, (string?)null) : await ResolveDiscountAsync(request.DiscountCode, subtotal, ct);
        return new(CheckoutOutcomeKind.Success, new QuoteSnapshot
        {
            Subtotal = subtotal,
            DiscountTotal = discount.Item1,
            ShippingTotal = area.ShippingFee,
            Total = Math.Max(0, subtotal - discount.Item1) + area.ShippingFee,
            CurrencyCode = currency
        }, discount.Item2);
    }
    private async Task<(decimal Amount, string? Message)> ResolveDiscountAsync(string code, decimal subtotal, CancellationToken ct)
    {
        if (!_boundary.DiscountsAllowed) return (0m, "Discount codes are unavailable at this store.");
        return DiscountPolicy.Calculate(await _store.DiscountAsync(code, ct), subtotal, _clock.UtcNow);
    }
}
