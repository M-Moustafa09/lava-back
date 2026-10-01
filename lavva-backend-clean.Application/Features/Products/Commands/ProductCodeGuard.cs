namespace lavva_backend_clean.Application.Features.Products.Commands;

/// <summary>
/// One CRM product ↔ one store product: the code decides the warehouse of every synced order line, so
/// two live products sharing a code would be indistinguishable to the CRM. A product's codes are its own
/// and its sizes' (each size is its own CRM product). Deleted products are hidden by the query filter,
/// so deleting a product frees its codes.
/// </summary>
internal static class ProductCodeGuard
{
    public static async Task<bool> IsTakenAsync(IApplicationDbContext context, string? productCode, IEnumerable<SizeCodeInput>? sizes, Guid? exceptProductId, CancellationToken ct)
    {
        var wanted = new[] { productCode }
            .Concat((sizes ?? Enumerable.Empty<SizeCodeInput>()).Select(s => s.ProductCode))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0) return false;

        // Size codes live in a JSON column, so the comparison runs here; the catalogue is small.
        var others = await context.Products
            .AsNoTracking()
            .Where(p => p.Id != exceptProductId)
            .ToListAsync(ct);
        return others.SelectMany(p => p.AllProductCodes()).Any(wanted.Contains);
    }
}

/// <summary>A size the product is sold in, with its CRM code (null = falls back to the product's code).</summary>
public record SizeCodeInput(string Size, string? ProductCode = null);
