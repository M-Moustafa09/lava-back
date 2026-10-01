using lavva_backend_clean.Domain.Entities.Orders;

namespace lavva_backend_clean.Infrastructure.Persistence.Configurations.Orders;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(o => o.PriceAtOrder);
        builder.Property(o => o.Size).HasMaxLength(20);
        builder.Property(o => o.ProductCode).HasMaxLength(64);
    }
}
