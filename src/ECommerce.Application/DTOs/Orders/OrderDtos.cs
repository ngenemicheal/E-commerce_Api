using ECommerce.Application.DTOs.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Orders;

public record OrderResponse(
    Guid Id,
    Guid CustomerId,
    List<OrderItemResponse> Items,
    MoneyDto TotalAmount,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? DeliveredAt);

public record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductNameSnapshot,
    MoneyDto UnitPriceSnapshot,
    int Quantity,
    MoneyDto LineTotal);

public record CheckoutRequest(
    Guid? CartId = null);

public record OrderStatusResponse(
    Guid OrderId,
    OrderStatus Status);
