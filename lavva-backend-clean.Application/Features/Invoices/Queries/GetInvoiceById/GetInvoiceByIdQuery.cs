using lavva_backend_clean.Application.Features.Invoices.Queries.GetInvoices;

namespace lavva_backend_clean.Application.Features.Invoices.Queries.GetInvoiceById;

public record GetInvoiceByIdQuery(Guid Id, Guid UserId, bool IsStaff) : IRequest<Result<GetInvoiceByIdResponse>>;

public record GetInvoiceByIdResponse(
    Guid Id,
    string InvoiceNumber,
    decimal TotalAmount,
    DateTime IssuedAt,
    InvoiceOrderDto Order
);
