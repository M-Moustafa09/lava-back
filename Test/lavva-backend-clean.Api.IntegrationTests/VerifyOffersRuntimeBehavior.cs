using System.Reflection;
using lavva_backend_clean.Application.Features.BundleOffers.Commands.CreateBundleOffer;
using lavva_backend_clean.Application.Features.BundleOffers.Commands.UpdateBundleOffer;
using lavva_backend_clean.Application.Features.BundleOffers.Queries.GetBundleOfferById;
using lavva_backend_clean.Application.Features.BundleOffers.Queries.GetBundleOffers;
using lavva_backend_clean.Application.Features.Offers.Commands.CreateOffer;
using lavva_backend_clean.Application.Features.Offers.Commands.UpdateOffer;
using lavva_backend_clean.Application.Features.Offers.Queries.GetOffers;
using lavva_backend_clean.Domain.Entities.Categories;
using lavva_backend_clean.Domain.Entities.Offers;
using lavva_backend_clean.Domain.Entities.Products;
using lavva_backend_clean.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace lavva_backend_clean.Api.IntegrationTests;

public class VerifyOffersRuntimeBehavior
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    private Microsoft.Extensions.Caching.Distributed.IDistributedCache GetCache()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions());
        return new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(opts);
    }

    [Fact]
    public async Task Verify_GetBundleOffers_And_InvalidState_Resave()
    {
        var db = GetDbContext();

        var category = Category.Create("Cat", "Cat", "cat", "");
        var product = Product.Create(category.Id, "Prod", "Prod", "", "prod", lavva_backend_clean.Domain.ValueObjects.Money.FromDecimal(100m), null, new List<string>(), new List<string>(), new List<string>());
        
        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var corruptedOffer = Offer.Create(product.Id, lavva_backend_clean.Domain.ValueObjects.Percentage.FromDecimal(50), lavva_backend_clean.Domain.ValueObjects.DateRange.Create(DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));
        
        var discountProp = typeof(Offer).GetProperty("Discount", BindingFlags.Public | BindingFlags.Instance);
        var unsafeMethod = typeof(lavva_backend_clean.Domain.ValueObjects.Percentage).GetMethod("UnsafeFromDatabase", BindingFlags.NonPublic | BindingFlags.Static);
        var badPercentage = unsafeMethod!.Invoke(null, new object[] { 110m });
        discountProp!.SetValue(corruptedOffer, badPercentage);
        
        db.Offers.Add(corruptedOffer);

        var bundle = BundleOffer.Create("BTitle", "BSub", lavva_backend_clean.Domain.ValueObjects.Money.FromDecimal(0), "bimg", lavva_backend_clean.Domain.ValueObjects.DateRange.Create(DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));
        bundle.AddItem(product.Id, 2); // 2 items * 100 = 200 original price
        db.BundleOffers.Add(bundle);

        await db.SaveChangesAsync();

        var getOffersHandler = new GetOffersHandler(db, null!);
        var offersResult = await getOffersHandler.Handle(new GetOffersQuery(false), CancellationToken.None);
        
        Assert.True(offersResult.IsSuccess);
        var offerResp = offersResult.Value.First();
        Assert.Equal(-10m, offerResp.Price);

        var getBundleOffersHandler = new GetBundleOffersHandler(db, GetCache());
        var bundleOffersResult = await getBundleOffersHandler.Handle(new GetBundleOffersQuery(false), CancellationToken.None);
        
        Assert.True(bundleOffersResult.IsSuccess);
        var bundleResp = bundleOffersResult.Value.First();
        Assert.Equal(0m, bundleResp.BundlePrice);
        Assert.Equal(200m, bundleResp.OriginalPrice); // 100 * 2
        Assert.Equal(200m, bundleResp.Saving); // 200 - 0
        Assert.Equal(100m, bundleResp.DiscountPercent); // (200/200)*100

        var updateOfferHandler = new UpdateOfferHandler(db);
        var updateCmd = new UpdateOfferCommand(corruptedOffer.Id, 110m, DateTime.UtcNow, DateTime.UtcNow.AddDays(2)); // user passes 110 back
        
        await Assert.ThrowsAsync<ArgumentException>(() => updateOfferHandler.Handle(updateCmd, CancellationToken.None));

        var validUpdateCmd = new UpdateOfferCommand(corruptedOffer.Id, 50m, DateTime.UtcNow, DateTime.UtcNow.AddDays(2));
        var validUpdateResult = await updateOfferHandler.Handle(validUpdateCmd, CancellationToken.None);
        
        Assert.True(validUpdateResult.IsSuccess);
        
        var healedOffer = await db.Offers.FindAsync(corruptedOffer.Id);
        Assert.Equal(50m, healedOffer!.Discount.Value);

        var createBundleHandler = new CreateBundleOfferHandler(db);
        var createCmd = new CreateBundleOfferCommand("New", "Sub", 0m, "img", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), new List<BundleItemDto> { new BundleItemDto(product.Id, 1) });
        var createResult = await createBundleHandler.Handle(createCmd, CancellationToken.None);
        
        Assert.True(createResult.IsSuccess);
        var savedNewBundle = await db.BundleOffers.FindAsync(createResult.Value);
        Assert.Equal(0m, savedNewBundle!.BundlePrice.Value);

        var createOfferHandler = new CreateOfferHandler(db);
        var badCreateOfferCmd = new CreateOfferCommand(product.Id, 110m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        await Assert.ThrowsAsync<ArgumentException>(() => createOfferHandler.Handle(badCreateOfferCmd, CancellationToken.None));

        var badCreateBundleCmd = new CreateBundleOfferCommand("New", "Sub", -10m, "img", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), new List<BundleItemDto> { new BundleItemDto(product.Id, 1) });
        var badCreateBundleResult = await createBundleHandler.Handle(badCreateBundleCmd, CancellationToken.None);
        Assert.False(badCreateBundleResult.IsSuccess);
        Assert.Equal("InvalidData", badCreateBundleResult.Error.Code);
    }
}
