using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Order? Order { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductNameSnapshot { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPriceSnapshot { get; private set; }

    public OrderItem(Guid id, Guid orderId, Guid productId, string productNameSnapshot,
        int quantity, Money unitPriceSnapshot)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Order item ID cannot be empty.", nameof(id));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(productNameSnapshot))
        {
            throw new ArgumentException(
                "Product name snapshot cannot be null or whitespace.",
                nameof(productNameSnapshot));
        }

        if (quantity < 1)
        {
            throw new InvalidCartQuantityException(quantity);
        }

        if (unitPriceSnapshot is null)
        {
            throw new ArgumentNullException(nameof(unitPriceSnapshot));
        }

        if (unitPriceSnapshot.Amount <= 0m)
        {
            throw new InvalidPriceException(unitPriceSnapshot.Amount, unitPriceSnapshot.Currency);
        }

        Id = id;
        OrderId = orderId;
        ProductId = productId;
        ProductNameSnapshot = productNameSnapshot.Trim();
        Quantity = quantity;
        UnitPriceSnapshot = unitPriceSnapshot;
    }

#pragma warning disable CS8618
    private OrderItem() { }
#pragma warning restore CS8618

    public Money CalculateLineTotal() => UnitPriceSnapshot.Multiply(Quantity);
}
