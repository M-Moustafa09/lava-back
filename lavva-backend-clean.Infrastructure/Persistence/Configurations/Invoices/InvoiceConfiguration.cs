using lavva_backend_clean.Domain.Entities.Invoices;

namespace lavva_backend_clean.Infrastructure.Persistence.Configurations.Invoices;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.Property(i => i.TotalAmount)
            .HasConversion(
                v => v.Value,
                v => lavva_backend_clean.Domain.ValueObjects.Money.UnsafeFromDatabase(v));
    }
}
