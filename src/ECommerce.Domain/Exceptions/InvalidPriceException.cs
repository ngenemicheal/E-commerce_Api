namespace ECommerce.Domain.Exceptions;

public class InvalidPriceException : DomainException
{
    public decimal Amount { get; }

    public InvalidPriceException(decimal amount, string currency)
        : base($"Price {amount} {currency} is invalid. Amount must be greater than zero.")
    {
        Amount = amount;
    }
}
