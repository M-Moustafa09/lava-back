namespace lavva_backend_clean.Domain.Entities.Products;

/// <summary>
/// The CRM code of one size of a product. In the CRM every size is its own product with its own code
/// (e.g. black shaper S = L54, M = L55), and the code decides which warehouse a synced order line ships
/// from, so the store must send the code of the size the customer chose — not one code per product.
/// </summary>
public class ProductSizeCode
{
    public string Size { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
}
