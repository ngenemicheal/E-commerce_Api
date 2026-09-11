namespace ECommerce.Domain.Exceptions;

public class EmptyCartOnCheckoutException : DomainException
{
    public Guid CartId { get; }

    public EmptyCartOnCheckoutException(Guid cartId)
        : base($"Cannot checkout cart {cartId} because it contains no items.")
    {
        CartId = cartId;
    }
}
