namespace ECommerce.Domain.Exceptions;

public class InvalidCartQuantityException : DomainException
{
    public int Quantity { get; }

    public InvalidCartQuantityException(int quantity)
        : base($"Cart item quantity must be greater than zero. Provided: {quantity}.")
    {
        Quantity = quantity;
    }
}
