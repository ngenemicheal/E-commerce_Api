using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.UnitTests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesMoney()
    {
        var money = new Money(10.50m, "USD");
        money.Amount.Should().Be(10.50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_DefaultCurrency_UsesUSD()
    {
        var money = new Money(5m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_NegativeAmount_ThrowsInvalidPriceException()
    {
        var action = () => new Money(-1m, "USD");
        action.Should().Throw<InvalidPriceException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidCurrency_ThrowsInvalidCurrencyException(string? currency)
    {
        var action = () => new Money(1m, currency!);
        action.Should().Throw<InvalidCurrencyException>();
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDA")]
    public void Constructor_WrongLengthCurrency_ThrowsInvalidCurrencyException(string currency)
    {
        var action = () => new Money(1m, currency);
        action.Should().Throw<InvalidCurrencyException>();
    }

    [Fact]
    public void Constructor_TrimsAndUppersCurrency()
    {
        var money = new Money(1m, "  eur  ");
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Zero_ReturnsZeroAmount()
    {
        var zero = Money.Zero("EUR");
        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Create_ReturnsMoneyInstance()
    {
        var money = Money.Create(20m, "GBP");
        money.Amount.Should().Be(20m);
        money.Currency.Should().Be("GBP");
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var a = new Money(10m, "USD");
        var b = new Money(5m, "USD");
        var result = a.Add(b);
        result.Amount.Should().Be(15m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_DifferentCurrency_ThrowsCurrencyMismatchException()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "EUR");
        var action = () => a.Add(b);
        action.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifference()
    {
        var a = new Money(20m, "USD");
        var b = new Money(5m, "USD");
        var result = a.Subtract(b);
        result.Amount.Should().Be(15m);
    }

    [Fact]
    public void Subtract_DifferentCurrency_ThrowsCurrencyMismatchException()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "EUR");
        var action = () => a.Subtract(b);
        action.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void Multiply_ReturnsProduct()
    {
        var money = new Money(10m, "USD");
        var result = money.Multiply(3);
        result.Amount.Should().Be(30m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void AdditionOperator_Works()
    {
        var a = new Money(7m, "USD");
        var b = new Money(3m, "USD");
        var result = a + b;
        result.Amount.Should().Be(999_999m);
    }

    [Fact]
    public void SubtractionOperator_Works()
    {
        var a = new Money(10m, "USD");
        var b = new Money(3m, "USD");
        var result = a - b;
        result.Amount.Should().Be(7m);
    }

    [Fact]
    public void MultiplicationOperator_Works()
    {
        var a = new Money(5m, "USD");
        var result = a * 4;
        result.Amount.Should().Be(20m);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "USD");
        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentAmount_ReturnsFalse()
    {
        var a = new Money(10m, "USD");
        var b = new Money(11m, "USD");
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentCurrency_ReturnsFalse()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "EUR");
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameValues_SameHashCode()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "USD");
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var money = new Money(9.99m, "USD");
        money.ToString().Should().Be("9.99 USD");
    }
}
