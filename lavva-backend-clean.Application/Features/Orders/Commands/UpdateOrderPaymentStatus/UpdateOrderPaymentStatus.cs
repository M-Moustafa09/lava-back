using lavva_backend_clean.Domain.Entities.Orders;

namespace lavva_backend_clean.Application.Features.Orders.Commands.UpdateOrderPaymentStatus;

public record UpdateOrderPaymentStatusCommand(Guid OrderId, PaymentStatus PaymentStatus, Guid UserId) : IRequest<Result>;

public class UpdateOrderPaymentStatusValidator : AbstractValidator<UpdateOrderPaymentStatusCommand>
{
    public UpdateOrderPaymentStatusValidator(Microsoft.Extensions.Localization.IStringLocalizer<lavva_backend_clean.Shared.Resources.SharedResource> localizer)
    {
        RuleFor(x => x.PaymentStatus)
            .IsInEnum().WithMessage(localizer["Validation_InvalidStatus"]);
    }
}

public class UpdateOrderPaymentStatusHandler : IRequestHandler<UpdateOrderPaymentStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateOrderPaymentStatusHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateOrderPaymentStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order is null) return Result.Failure(new Error("Error.NotFound", "Order_NotFound"));

        var oldStatus = order.PaymentStatus.ToString();
        order.ChangePaymentStatus(request.PaymentStatus);

        _context.OrderEditLogs.Add(new OrderEditLog
        {
            OrderId = order.Id,
            EditedBy = request.UserId,
            FieldName = "PaymentStatus",
            OldValue = oldStatus,
            NewValue = request.PaymentStatus.ToString(),
            EditedAt = DateTime.UtcNow
        });

        _context.Orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
