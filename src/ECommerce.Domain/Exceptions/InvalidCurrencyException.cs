namespace ECommerce.Domain.Exceptions;

public class InvalidCurrencyException : DomainException
{
    public string Currency { get; }

    public InvalidCurrencyException(string currency)
        : base($"Currency '{currency}' is invalid or not supported.")
    {
        Currency = currency;
    }
}
