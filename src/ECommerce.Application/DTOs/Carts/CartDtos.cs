using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.DTOs.Carts;

public record CartResponse(
    Guid Id,
    Guid CustomerId,
    List<CartItemResponse> Items,
    MoneyDto Total,
    int ItemCount,
    DateTimeOffset UpdatedAt);

public record CartItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    MoneyDto UnitPrice,
    int Quantity,
    MoneyDto LineTotal);

public record AddCartItemRequest(
    Guid ProductId,
    int Quantity);

public record UpdateCartItemQuantityRequest(
    int Quantity);
