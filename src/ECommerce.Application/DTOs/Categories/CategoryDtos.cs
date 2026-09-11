namespace ECommerce.Application.DTOs.Categories;

public record CategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    int ProductCount = 0);

public record CategoryDetailResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateCategoryRequest(
    string Name,
    string Slug,
    string? Description);

public record UpdateCategoryRequest(
    string Name,
    string Slug,
    string? Description);
