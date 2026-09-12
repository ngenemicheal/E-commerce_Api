using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class CartItem
{
    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public Cart? Cart { get; private set; }
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }

    public CartItem(Guid id, Guid cartId, Guid productId, int quantity, Money unitPrice)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Cart item ID cannot be empty.", nameof(id));
        }

        if (cartId == Guid.Empty)
        {
            throw new ArgumentException("Cart ID cannot be empty.", nameof(cartId));
        }

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

        Id = id;
        CartId = cartId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

#pragma warning disable CS8618
    private CartItem() { }
#pragma warning restore CS8618

    public void ChangeQuantity(int quantity)
    {
        if (quantity < 1)
        {
            throw new InvalidCartQuantityException(quantity);
        }

        Quantity = quantity;
    }

    public Money CalculateLineTotal() => UnitPrice.Multiply(Quantity);
}
