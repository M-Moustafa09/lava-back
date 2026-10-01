namespace lavva_backend_clean.Application.Features.Invoices.Queries.GetInvoicePdf;

public record GetInvoicePdfQuery(Guid Id) : IRequest<Result<byte[]>>;

public class GetInvoicePdfHandler : IRequestHandler<GetInvoicePdfQuery, Result<byte[]>>
{
    private readonly IApplicationDbContext _context;
    private readonly IInvoicePdfGenerator _pdfGenerator;
    public GetInvoicePdfHandler(IApplicationDbContext context, IInvoicePdfGenerator pdfGenerator) { _context = context; _pdfGenerator = pdfGenerator; }

    public async Task<Result<byte[]>> Handle(GetInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Order).ThenInclude(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(i => i.Order).ThenInclude(o => o.Customer)
            .Include(i => i.Order).ThenInclude(o => o.Country)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == request.Id && !i.IsDeleted, cancellationToken);
        if (invoice == null) return Result.Failure<byte[]>(new Error("Error.NotFound", "Invoice_NotFound"));
        var pdfBytes = await _pdfGenerator.GeneratePdfAsync(invoice, cancellationToken);
        return Result.Success(pdfBytes);
    }
}
