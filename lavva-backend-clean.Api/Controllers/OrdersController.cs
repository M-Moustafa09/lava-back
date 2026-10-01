using lavva_backend_clean.Application.Features.Orders.Commands.CreateOrder;
using lavva_backend_clean.Application.Features.Orders.Commands.UpdateOrder;
using lavva_backend_clean.Application.Features.Orders.Commands.UpdateOrderPaymentStatus;
using lavva_backend_clean.Application.Features.Orders.Commands.UpdateOrderStatus;
using lavva_backend_clean.Application.Features.Orders.Queries.GetOrderById;
using lavva_backend_clean.Application.Features.Orders.Queries.GetOrderEditLogs;
using lavva_backend_clean.Application.Features.Orders.Queries.GetOrders;
using lavva_backend_clean.Application.Features.Orders.Queries.TrackOrder;
using lavva_backend_clean.Domain.Enums;
using lavva_backend_clean.Shared;

namespace lavva_backend_clean.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    public OrdersController(ISender sender) { _sender = sender; }

    [HttpGet]
    [Authorize(Roles = "Admin,StoreManager,SalesEmployee")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default) =>
        (await _sender.Send(new GetOrdersQuery(status, null, null, search, null, null, page, pageSize), ct)).ToApiResponse();

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,StoreManager,SalesEmployee")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) => (await _sender.Send(new GetOrderByIdQuery(id), ct)).ToApiResponse();

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand cmd, CancellationToken ct) => (await _sender.Send(cmd, ct)).ToApiResponse();

    [HttpPost("guest")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateGuestOrder([FromBody] CreateOrderCommand cmd, CancellationToken ct) => (await _sender.Send(cmd, ct)).ToApiResponse();

    [HttpPatch("{id}/status")]
    [HttpPatch("{id}")]
    [Authorize(Roles = "Admin,StoreManager,SalesEmployee")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct) {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value ?? Guid.Empty.ToString());
        if (request.Status is null && request.PaymentStatus is null)
            return Result.Failure(new Error("Error.Validation", "Validation_InvalidStatus")).ToApiResponse();

        if (request.Status is OrderStatus status)
        {
            var statusResult = await _sender.Send(new UpdateOrderStatusCommand(id, status, userId), ct);
            if (statusResult.IsFailure || request.PaymentStatus is null) return statusResult.ToApiResponse();
        }

        return (await _sender.Send(new UpdateOrderPaymentStatusCommand(id, request.PaymentStatus!.Value, userId), ct)).ToApiResponse();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrderCommand cmd, CancellationToken ct) {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value ?? Guid.Empty.ToString());
        return (await _sender.Send(cmd with { Id = id, EditorId = userId }, ct)).ToApiResponse();
    }

    [HttpGet("{id}/edit-logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetEditLogs(Guid id, CancellationToken ct) => (await _sender.Send(new GetOrderEditLogsQuery(id), ct)).ToApiResponse();

    [HttpPost("track")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackOrder([FromBody] TrackOrderQuery query, CancellationToken ct) => (await _sender.Send(query, ct)).ToApiResponse();

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> DeleteOrder(Guid id, CancellationToken ct) => (await _sender.Send(new lavva_backend_clean.Application.Features.Orders.Commands.DeleteOrder.DeleteOrderCommand(id), ct)).ToApiResponse();

    [HttpPost("bulk-status")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] lavva_backend_clean.Application.Features.Orders.Commands.BulkUpdateOrderStatus.BulkUpdateOrderStatusCommand cmd, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value ?? Guid.Empty.ToString());
        return (await _sender.Send(cmd with { UserId = userId }, ct)).ToApiResponse();
    }
}

public record UpdateOrderStatusRequest(
    [property: System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))] OrderStatus? Status,
    [property: System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))] PaymentStatus? PaymentStatus);
