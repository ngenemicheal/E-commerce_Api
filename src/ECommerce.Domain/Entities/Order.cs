using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order(Guid id, Guid customerId, Money totalAmount, DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
        CreatedAt = createdAt;
    }

    public static Order CreateFromCart(Guid id, Guid customerId, Cart cart, DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Order ID cannot be empty.", nameof(id));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer ID cannot be empty.", nameof(customerId));
        }

        if (cart is null)
        {
            throw new ArgumentNullException(nameof(cart));
        }

        if (cart.CustomerId != customerId)
        {
            throw new InvalidOperationException(
                $"Cart {cart.Id} does not belong to customer {customerId}.");
        }

        if (cart.IsEmpty)
        {
            throw new EmptyCartOnCheckoutException(cart.Id);
        }

        var order = new Order(id, customerId, cart.CalculateTotal(), now);

        foreach (var cartItem in cart.Items)
        {
            order._items.Add(new OrderItem(
                Guid.NewGuid(),
                order.Id,
                cartItem.ProductId,
                GetProductNameOrFallback(cartItem),
                cartItem.Quantity,
                cartItem.UnitPrice));
        }

        return order;
    }

    private static string GetProductNameOrFallback(CartItem cartItem)
    {
        return cartItem.Product?.Name ?? $"Product-{cartItem.ProductId}";
    }

    public void SetPaid(DateTimeOffset now)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Paid);
        }

        Status = OrderStatus.Paid;
        PaidAt = now;
    }

    public void SetShipped(DateTimeOffset now)
    {
        if (Status != OrderStatus.Paid)
        {
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Shipped);
        }

        Status = OrderStatus.Shipped;
        ShippedAt = now;
    }

    public void SetDelivered(DateTimeOffset now)
    {
        if (Status != OrderStatus.Shipped)
        {
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Delivered);
        }

        Status = OrderStatus.Delivered;
        DeliveredAt = now;
    }

    public void Cancel()
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.Paid))
        {
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Cancelled);
        }

        Status = OrderStatus.Cancelled;
    }
}
