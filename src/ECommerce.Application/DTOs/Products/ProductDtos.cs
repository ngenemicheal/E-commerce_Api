using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.DTOs.Products;

public record ProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    MoneyDto Price,
    Guid CategoryId,
    string? CategoryName,
    int StockQuantity,
    string? ImageUrl,
    DateTimeOffset CreatedAt);

public record ProductDetailResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    MoneyDto Price,
    Guid CategoryId,
    CategoryResponse? Category,
    int StockQuantity,
    string? ImageUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

// public record CreateProductRequest(
//     string Name,
//     string Slug,
//     string? Description,
//     decimal PriceAmount,
//     string PriceCurrency = "USD",
//     // Guid CategoryId = default,
//     // string CategoryId = "",
//     Guid CategoryId,
//     int StockQuantity = 0,
//     string? ImageUrl = null);

public record CreateProductRequest(
    string Name,
    string Slug,
    string? Description,
    decimal PriceAmount,
    Guid CategoryId,
    string PriceCurrency = "USD",
    int StockQuantity = 0,
    string? ImageUrl = null);

// public record UpdateProductRequest(
//     string Name,
//     string Slug,
//     string? Description,
//     decimal PriceAmount,
//     string PriceCurrency = "USD",
//     Guid CategoryId = default,
//     int StockQuantity = 0,
//     string? ImageUrl = null);

public record UpdateProductRequest(
    string Name,
    string Slug,
    string? Description,
    decimal PriceAmount,
    Guid CategoryId,
    string PriceCurrency = "USD",
    int StockQuantity = 0,
    string? ImageUrl = null);