using lavva_backend_clean.Domain.Entities.Orders;
using lavva_backend_clean.Application.Features.Offers;
using lavva_backend_clean.Application.Features.Orders.Commands.CreateOrder;

namespace lavva_backend_clean.Application.Features.Orders.Commands.UpdateOrder;

public record UpdateOrderCommand(
    Guid Id,
    string CustomerName,
    string Phone,
    string Address,
    string? ShipmentCode,
    Guid EditorId,
    string? City = null,
    string? Area = null,
    string? DeliveryCompany = null,
    string? Notes = null,
    PaymentMethod? PaymentMethod = null,
    List<OrderItemDto>? Items = null
) : IRequest<Result>;

public class UpdateOrderValidator : AbstractValidator<UpdateOrderCommand>
{
    private static readonly PaymentMethod[] EditablePaymentMethods = { PaymentMethod.CashOnDelivery, PaymentMethod.BankTransfer, PaymentMethod.DebitCard };

    public UpdateOrderValidator(Microsoft.Extensions.Localization.IStringLocalizer<lavva_backend_clean.Shared.Resources.SharedResource> localizer)
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(localizer["Validation_Required"]);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage(localizer["Validation_Required"]);

        RuleFor(x => x.PaymentMethod)
            .Must(m => m is null || EditablePaymentMethods.Contains(m.Value))
            .WithMessage(localizer["Order_PaymentMethodNotSupported"]);

        RuleFor(x => x.Items)
            .Must(items => items is null || items.Count > 0)
            .WithMessage(localizer["Order_EmptyItems"]);

        RuleForEach(x => x.Items)
            .Must(item => item.Quantity > 0)
            .WithMessage(localizer["Validation_GreaterThanZero"]);
    }
}

public class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache _cache;

    public UpdateOrderHandler(IApplicationDbContext context, Microsoft.Extensions.Caching.Distributed.IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
        if (order == null) return Result.Failure(new Error("Error.NotFound", "Order_NotFound"));

        order.UpdateEditableDetails(
            request.CustomerName,
            request.Phone,
            request.Address,
            request.ShipmentCode,
            request.City,
            request.Area,
            request.DeliveryCompany,
            request.Notes);
        AddLog(order.Id, request.EditorId, "Details", null, "Updated");

        if (request.PaymentMethod is PaymentMethod paymentMethod && paymentMethod != order.PaymentMethod)
        {
            AddLog(order.Id, request.EditorId, "PaymentMethod", order.PaymentMethod.ToString(), paymentMethod.ToString());
            order.ChangePaymentMethod(paymentMethod);
        }

        if (request.Items is not null)
        {
            var itemsResult = await ReplaceItemsAsync(order, request, cancellationToken);
            if (itemsResult.IsFailure) return itemsResult;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// A line already on the order keeps the price the customer was charged; a newly added product is
    /// priced like checkout prices it: the order country's price, less any running offer.
    /// </summary>
    private async Task<Result> ReplaceItemsAsync(Order order, UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var requestedIds = request.Items!.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .IgnoreQueryFilters()
            .Where(p => requestedIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // One line per product and size. A size the product is not sold in is refused, as at checkout.
        var sized = new List<(Guid ProductId, string? Size, int Quantity)>();
        foreach (var item in request.Items!)
        {
            string? size = null;
            if (products.TryGetValue(item.ProductId, out var product) && !product.TryMatchSize(item.Size, out size))
                return Result.Failure(new Error("Error.Validation", "Order_SizeNotAvailable"));
            sized.Add((item.ProductId, size, item.Quantity));
        }
        var lines = sized
            .GroupBy(l => (l.ProductId, Size: l.Size?.ToUpperInvariant()))
            .Select(g => (ProductId: g.Key.ProductId, Size: g.First().Size, Quantity: g.Sum(l => l.Quantity)))
            .ToList();

        // A line already on the order keeps the CRM code it was taken with; a new one takes its size's code now.
        var existingCodes = order.OrderItems
            .GroupBy(i => (i.ProductId, Size: i.Size?.ToUpperInvariant()))
            .ToDictionary(g => g.Key, g => g.First().ProductCode);

        var chargedPrices = order.OrderItems
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.First().PriceAtOrder);

        var newProductIds = lines.Select(l => l.ProductId).Where(id => !chargedPrices.ContainsKey(id)).ToList();
        var countryPrices = await _context.ProductPrices
            .Where(p => newProductIds.Contains(p.ProductId) && p.CountryId == order.CountryId && !p.Product.IsDeleted)
            .ToDictionaryAsync(p => p.ProductId, p => p.Price, cancellationToken);
        var offers = newProductIds.Count > 0
            ? await ActiveOffers.GetAsync(_context, _cache, cancellationToken)
            : new Dictionary<Guid, ActiveOfferDto>();

        var priced = new List<(Guid ProductId, int Quantity, decimal PriceAtOrder, string? Size, string? ProductCode)>();
        foreach (var (productId, size, quantity) in lines)
        {
            var code = existingCodes.TryGetValue((productId, size?.ToUpperInvariant()), out var keptCode)
                ? keptCode
                : products.GetValueOrDefault(productId)?.ResolveProductCode(size);

            if (chargedPrices.TryGetValue(productId, out var charged))
            {
                priced.Add((productId, quantity, charged, size, code));
                continue;
            }

            if (!countryPrices.TryGetValue(productId, out var price))
                return Result.Failure(new Error("Error.Validation", "Order_ProductNotSoldInCountry"));
            if (offers.TryGetValue(productId, out var offer))
                price = ActiveOffers.Apply(price, offer.Percent);
            priced.Add((productId, quantity, price, size, code));
        }

        var oldSummary = Summarize(order.OrderItems.Select(i => (i.ProductId, i.Size, i.Quantity)));
        var oldTotal = order.TotalAmount.Value;

        _context.OrderItems.RemoveRange(order.OrderItems.ToList());
        order.ReplaceItems(priced);
        foreach (var item in order.OrderItems)
        {
            _context.OrderItems.Add(item);
        }

        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == order.Id, cancellationToken);
        invoice?.UpdateTotal(order.TotalAmount);

        AddLog(order.Id, request.EditorId, "Items", oldSummary, Summarize(priced.Select(p => (p.ProductId, p.Size, p.Quantity))));
        if (oldTotal != order.TotalAmount.Value)
            AddLog(order.Id, request.EditorId, "TotalAmount", oldTotal.ToString("0.##"), order.TotalAmount.Value.ToString("0.##"));

        return Result.Success();
    }

    private static string Summarize(IEnumerable<(Guid ProductId, string? Size, int Quantity)> lines) =>
        string.Join(", ", lines.Select(l => l.Size is null ? $"{l.ProductId}x{l.Quantity}" : $"{l.ProductId}/{l.Size}x{l.Quantity}"));

    private void AddLog(Guid orderId, Guid editorId, string field, string? oldValue, string newValue)
    {
        _context.OrderEditLogs.Add(new OrderEditLog
        {
            OrderId = orderId,
            EditedBy = editorId,
            FieldName = field,
            OldValue = oldValue,
            NewValue = newValue,
            EditedAt = DateTime.UtcNow
        });
    }
}
