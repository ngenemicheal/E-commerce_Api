using ECommerce.Application.DTOs.Carts;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Domain.Entities;
using ECommerce.Domain.ValueObjects;
using Mapster;

namespace ECommerce.Application.Mappings;

public sealed class CommerceMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<CartItem, CartItemResponse>()
            .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : $"Product-{src.ProductId}")
            .Map(dest => dest.UnitPrice, src => src.UnitPrice.Adapt<MoneyDto>())
            .Map(dest => dest.LineTotal, src => src.CalculateLineTotal().Adapt<MoneyDto>());

        config.ForType<Cart, CartResponse>()
            .Map(dest => dest.Items, src => src.Items.Adapt<List<CartItemResponse>>())
            .Map(dest => dest.Total, src => src.CalculateTotal().Adapt<MoneyDto>())
            .Map(dest => dest.ItemCount, src => src.Items != null ? src.Items.Sum(i => i.Quantity) : 0);

        config.ForType<OrderItem, OrderItemResponse>()
            .Map(dest => dest.UnitPriceSnapshot, src => src.UnitPriceSnapshot.Adapt<MoneyDto>())
            .Map(dest => dest.LineTotal, src => src.CalculateLineTotal().Adapt<MoneyDto>());

        config.ForType<Order, OrderResponse>()
            .Map(dest => dest.Items, src => src.Items.Adapt<List<OrderItemResponse>>())
            .Map(dest => dest.TotalAmount, src => src.TotalAmount.Adapt<MoneyDto>());
    }
}
