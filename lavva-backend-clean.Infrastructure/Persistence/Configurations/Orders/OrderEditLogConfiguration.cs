using lavva_backend_clean.Domain.Entities.Orders;

namespace lavva_backend_clean.Infrastructure.Persistence.Configurations.Orders;

public class OrderEditLogConfiguration : IEntityTypeConfiguration<OrderEditLog>
{
    public void Configure(EntityTypeBuilder<OrderEditLog> builder)
    {
        builder.HasOne(l => l.Editor)
            .WithMany()
            .HasForeignKey(l => l.EditedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
