namespace lavva_backend_clean.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(
    Guid CategoryId,
    string NameAr,
    string NameEn,
    string Description,
    List<string> Images,
    string? Features,
    string? ShippingPolicy,
    string? ReturnPolicy,
    decimal BasePrice,
    string? ProductCode = null,
    // Per-country pricing (2026-09-21): BasePrice is the international USD price.
    decimal? InternationalOriginalPrice = null,
    List<CountryPriceInput>? CountryPrices = null,
    // The sizes the product is sold in, each with its CRM code (2026-10-01). Null = no sizes.
    List<SizeCodeInput>? Sizes = null
) : IRequest<Result<CreateProductResponse>>;
