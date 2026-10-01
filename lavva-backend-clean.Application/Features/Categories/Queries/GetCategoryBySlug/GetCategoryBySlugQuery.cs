using lavva_backend_clean.Application.Features.Categories.Queries.GetCategories;

namespace lavva_backend_clean.Application.Features.Categories.Queries.GetCategoryBySlug;

public record GetCategoryBySlugQuery(string Slug) : IRequest<Result<CategoryListResponse>>;
