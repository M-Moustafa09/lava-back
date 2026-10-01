using System.Reflection;
using System.Text.Json;
using lavva_backend_clean.Domain.Entities.Inventory;
using lavva_backend_clean.Domain.Entities.Products;
using lavva_backend_clean.Domain.ValueObjects;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace lavva_backend_clean.Infrastructure.Persistence.Seeder.Products;

public class ProductSeeder : IDataSeeder
{
    private const string SeedResourceSuffix = "lava-products.json";

    private static readonly HashSet<string> LaunchCountryCodes = new(StringComparer.OrdinalIgnoreCase) { "EG", "SA", "AE" };

    public int Order => 5;

    public async Task SeedAsync(SeedContext context, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = serviceProvider.GetRequiredService<ILogger<ProductSeeder>>();

        using var document = JsonDocument.Parse(ReadSeedFile());

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var slug = item.GetProperty("slug").GetString()!;
            var categorySlug = item.GetProperty("category").GetString()!;
            var category = context.Categories.First(c => c.Slug == categorySlug);

            var existing = await dbContext.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
            if (existing == null)
            {
                var price = item.GetProperty("price").GetDecimal();
                var originalPrice = item.TryGetProperty("originalPrice", out var op) && op.ValueKind == JsonValueKind.Number ? op.GetDecimal() : (decimal?)null;

                var product = Product.Create(
                    categoryId: category.Id,
                    nameAr: item.GetProperty("nameAr").GetString()!,
                    nameEn: item.GetProperty("nameEn").GetString()!,
                    description: item.GetProperty("descAr").GetString() ?? string.Empty,
                    slug: slug,
                    basePrice: Money.FromDecimal(price),
                    originalPrice: originalPrice.HasValue ? Money.FromDecimal(originalPrice.Value) : null,
                    images: ReadStrings(item, "images"),
                    sizes: ReadStrings(item, "sizes"),
                    colors: ReadStrings(item, "colors"),
                    features: null,
                    shippingPolicy: null,
                    returnPolicy: null,
                    sizeChartJson: item.TryGetProperty("sizeChart", out var chart) ? chart.GetRawText() : null,
                    isNew: item.TryGetProperty("isNew", out var isNew) && isNew.GetBoolean(),
                    isBestSeller: item.TryGetProperty("isBestSeller", out var best) && best.GetBoolean(),
                    badge: item.TryGetProperty("badge", out var badge) ? badge.GetString() : null);

                product.Id = Guid.Parse(item.GetProperty("guid").GetString()!);
                product.SetInternationalPrice(price, originalPrice);
                product.UpdateStock(item.GetProperty("stock").GetInt32());
                product.UpdateRating(item.GetProperty("rating").GetDouble(), item.GetProperty("reviewCount").GetInt32());

                await dbContext.Products.AddAsync(product, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Seeded product '{NameEn}' ({Slug}).", product.NameEn, slug);
                existing = product;
            }

            var stock = item.GetProperty("stock").GetInt32();
            var seedPrice = item.GetProperty("price").GetDecimal();
            var seedOriginalPrice = item.TryGetProperty("originalPrice", out var seedOp) && seedOp.ValueKind == JsonValueKind.Number ? seedOp.GetDecimal() : (decimal?)null;

            foreach (var country in context.Countries)
            {
                if (LaunchCountryCodes.Contains(country.Code))
                {
                    var priceExists = await dbContext.ProductPrices.AnyAsync(
                        pr => pr.ProductId == existing.Id && pr.CountryId == country.Id,
                        cancellationToken);

                    if (!priceExists)
                    {
                        await dbContext.ProductPrices.AddAsync(
                            new ProductPrice { ProductId = existing.Id, CountryId = country.Id, Price = seedPrice, OriginalPrice = seedOriginalPrice },
                            cancellationToken);
                    }
                }

                var inventoryExists = await dbContext.InventoryItems.AnyAsync(
                    inv => inv.ProductId == existing.Id && inv.CountryId == country.Id,
                    cancellationToken);

                if (!inventoryExists)
                {
                    await dbContext.InventoryItems.AddAsync(InventoryItem.Create(existing.Id, country.Id, stock), cancellationToken);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        context.Products = await dbContext.Products.ToListAsync(cancellationToken);
    }

    private static string ReadSeedFile()
    {
        var assembly = typeof(ProductSeeder).Assembly;
        var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith(SeedResourceSuffix, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static List<string> ReadStrings(JsonElement item, string property)
    {
        return item.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray().Select(e => e.GetString()!).ToList()
            : new List<string>();
    }
}
