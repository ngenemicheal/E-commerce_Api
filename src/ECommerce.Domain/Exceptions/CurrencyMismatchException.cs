namespace ECommerce.Domain.Exceptions;

public class CurrencyMismatchException : DomainException
{
    public string LeftCurrency { get; }
    public string RightCurrency { get; }

    public CurrencyMismatchException(string leftCurrency, string rightCurrency)
        : base($"Cannot operate on amounts with different currencies: {leftCurrency} vs {rightCurrency}.")
    {
        LeftCurrency = leftCurrency;
        RightCurrency = rightCurrency;
    }
}
