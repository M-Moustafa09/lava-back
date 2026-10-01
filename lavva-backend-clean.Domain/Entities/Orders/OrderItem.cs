using lavva_backend_clean.Domain.Common;
using lavva_backend_clean.Domain.Entities.Products;
namespace lavva_backend_clean.Domain.Entities.Orders;

public class OrderItem : BaseEntity {
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal PriceAtOrder { get; set; }

    // The size the customer chose, and the CRM code it resolved to when the order was placed. The code is
    // kept on the line so remapping a product's codes later does not change orders already taken.
    public string? Size { get; set; }
    public string? ProductCode { get; set; }
}
