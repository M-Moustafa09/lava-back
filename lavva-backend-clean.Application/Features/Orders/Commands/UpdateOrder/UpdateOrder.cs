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
        var lines = request.Items!
            .GroupBy(i => i.ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(i => i.Quantity)))
            .ToList();

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

        var priced = new List<(Guid ProductId, int Quantity, decimal PriceAtOrder)>();
        foreach (var (productId, quantity) in lines)
        {
            if (chargedPrices.TryGetValue(productId, out var charged))
            {
                priced.Add((productId, quantity, charged));
                continue;
            }

            if (!countryPrices.TryGetValue(productId, out var price))
                return Result.Failure(new Error("Error.Validation", "Order_ProductNotSoldInCountry"));
            if (offers.TryGetValue(productId, out var offer))
                price = ActiveOffers.Apply(price, offer.Percent);
            priced.Add((productId, quantity, price));
        }

        var oldSummary = Summarize(order.OrderItems.Select(i => (i.ProductId, i.Quantity)));
        var oldTotal = order.TotalAmount.Value;

        _context.OrderItems.RemoveRange(order.OrderItems.ToList());
        order.ReplaceItems(priced);
        foreach (var item in order.OrderItems)
        {
            _context.OrderItems.Add(item);
        }

        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == order.Id, cancellationToken);
        invoice?.UpdateTotal(order.TotalAmount);

        AddLog(order.Id, request.EditorId, "Items", oldSummary, Summarize(priced.Select(p => (p.ProductId, p.Quantity))));
        if (oldTotal != order.TotalAmount.Value)
            AddLog(order.Id, request.EditorId, "TotalAmount", oldTotal.ToString("0.##"), order.TotalAmount.Value.ToString("0.##"));

        return Result.Success();
    }

    private static string Summarize(IEnumerable<(Guid ProductId, int Quantity)> lines) =>
        string.Join(", ", lines.Select(l => $"{l.ProductId}x{l.Quantity}"));

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
