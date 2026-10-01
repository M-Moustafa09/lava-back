using lavva_backend_clean.Domain.Entities.Categories;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace lavva_backend_clean.Infrastructure.Persistence.Seeder.Categories;

public class CategorySeeder : IDataSeeder
{
    public int Order => 4;

    public async Task SeedAsync(SeedContext context, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = serviceProvider.GetRequiredService<ILogger<CategorySeeder>>();

        var categoriesToSeed = new (string NameAr, string NameEn, string Slug, string ImageUrl)[]
        {
            ("مشدات الخصر", "Waist Trainers", "waist-trainers", "https://images.unsplash.com/photo-1571019614242-c5c5dee9f50b?w=400&h=400&fit=crop&auto=format"),
            ("مشدات الجسم", "Body Shapers", "body-shapers", "https://images.unsplash.com/photo-1544966503-7cc5ac882d5f?w=400&h=400&fit=crop&auto=format"),
            ("مشدات ما بعد الولادة", "Postpartum Shapers", "postpartum", "https://images.unsplash.com/photo-1555252333-9f8e92e65df9?w=400&h=400&fit=crop&auto=format"),
            ("سترات الضغط", "Compression Vests", "compression-vests", "https://images.unsplash.com/photo-1518611012118-696072aa579a?w=400&h=400&fit=crop&auto=format"),
            ("رافعات الأرداف", "Butt Lifters", "butt-lifters", "https://images.unsplash.com/photo-1506629082955-511b1aa562c8?w=400&h=400&fit=crop&auto=format"),
            ("إكسسوارات", "Accessories", "accessories", "https://images.unsplash.com/photo-1590736969955-71cc94901144?w=400&h=400&fit=crop&auto=format")
        };

        foreach (var (nameAr, nameEn, slug, imageUrl) in categoriesToSeed)
        {
            var existing = await dbContext.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
            if (existing == null)
            {
                var category = Category.Create(nameAr, nameEn, slug, imageUrl);
                await dbContext.Categories.AddAsync(category, cancellationToken);
                logger.LogInformation("Seeded category '{NameEn}' ({Slug}).", nameEn, slug);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        context.Categories = await dbContext.Categories.IgnoreQueryFilters().ToListAsync(cancellationToken);
    }
}
