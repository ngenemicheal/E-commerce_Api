using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Domain.Entities;
using ECommerce.Domain.ValueObjects;
using Mapster;

namespace ECommerce.Application.Mappings;

public sealed class CatalogMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<Money, MoneyDto>()
            .Map(dest => dest.Amount, src => src.Amount)
            .Map(dest => dest.Currency, src => src.Currency);

        config.ForType<Category, CategoryResponse>()
            .Map(dest => dest.ProductCount,
                src => src.Products != null ? src.Products.Count : 0);

        config.ForType<Category, CategoryDetailResponse>();

        config.ForType<Product, ProductResponse>()
            .Map(dest => dest.Price, src => src.Price.Adapt<MoneyDto>())
            .Map(dest => dest.CategoryName,
                src => src.Category != null ? src.Category.Name : null);

        config.ForType<Product, ProductDetailResponse>()
            .Map(dest => dest.Price, src => src.Price.Adapt<MoneyDto>());
    }
}
