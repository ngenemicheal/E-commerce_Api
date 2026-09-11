using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.ValueObjects;

public sealed class Money : IEquatable<Money>
{
    public const string DefaultCurrency = "USD";

    public decimal Amount { get; private init; }
    public string Currency { get; private init; }

    public Money(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
        {
            throw new InvalidPriceException(amount, currency);
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new InvalidCurrencyException(currency);
        }

        if (currency.Length != 3)
        {
            throw new InvalidCurrencyException(currency);
        }

        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public static Money Create(decimal amount, string currency = DefaultCurrency) => new(amount, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new CurrencyMismatchException(Currency, other.Currency);
        }
    }

    public bool Equals(Money? other)
    {
        if (ReferenceEquals(null, other)) return false;

        if (ReferenceEquals(this, other)) return true;

        return Amount == other.Amount && string.Equals(Currency, other.Currency, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is Money other && Equals(other));

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount:0.00} {Currency}";

    public static bool operator ==(Money? left, Money? right) => Equals(left, right);
    public static bool operator !=(Money? left, Money? right) => !Equals(left, right);

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money left, decimal factor) => left.Multiply(factor);
}
