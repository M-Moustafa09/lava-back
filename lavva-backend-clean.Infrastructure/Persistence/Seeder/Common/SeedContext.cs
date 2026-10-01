using lavva_backend_clean.Domain.Entities.Categories;
using lavva_backend_clean.Domain.Entities.Countries;
using lavva_backend_clean.Domain.Entities.Products;
using lavva_backend_clean.Domain.Entities.Users;

namespace lavva_backend_clean.Infrastructure.Persistence.Seeder.Common;

public class SeedContext
{
    public Country DefaultCountry { get; set; } = null!;
    public List<Country> Countries { get; set; } = new();

    public User AdminUser { get; set; } = null!;
    public User StoreManagerUser { get; set; } = null!;
    public User SalesEmployeeUser { get; set; } = null!;
    public User CustomerUser { get; set; } = null!;

    public List<Category> Categories { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}
