using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class Cart
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<CartItem> _items = new();
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    public Cart(Guid id, Guid customerId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Cart ID cannot be empty.", nameof(id));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer ID cannot be empty.", nameof(customerId));
        }

        Id = id;
        CustomerId = customerId;
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void AddItem(Guid productId, int quantity, Money unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        }

        if (quantity < 1)
        {
            throw new InvalidCartQuantityException(quantity);
        }

        if (unitPrice is null)
        {
            throw new ArgumentNullException(nameof(unitPrice));
        }

        if (unitPrice.Amount <= 0m)
        {
            throw new InvalidPriceException(unitPrice.Amount, unitPrice.Currency);
        }

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.ChangeQuantity(existing.Quantity + quantity);
        }
        else
        {
            _items.Add(new CartItem(Guid.NewGuid(), Id, productId, quantity, unitPrice));
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateQuantity(Guid productId, int quantity)
    {
        if (quantity < 0)
        {
            throw new InvalidCartQuantityException(quantity);
        }

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is null)
        {
            return;
        }

        if (quantity == 0)
        {
            _items.Remove(existing);
        }
        else
        {
            existing.ChangeQuantity(quantity);
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveItem(Guid productId)
    {
        var removed = _items.RemoveAll(i => i.ProductId == productId);
        if (removed > 0)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void Clear()
    {
        if (_items.Count > 0)
        {
            _items.Clear();
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public Money CalculateTotal()
    {
        if (_items.Count == 0)
        {
            return Money.Zero();
        }

        Money total = Money.Zero(_items[0].UnitPrice.Currency);
        foreach (var item in _items)
        {
            total = total.Add(item.CalculateLineTotal());
        }

        return total;
    }

    public bool IsEmpty => _items.Count == 0;
}
