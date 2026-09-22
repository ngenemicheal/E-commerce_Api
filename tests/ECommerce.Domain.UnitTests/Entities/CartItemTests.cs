using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.UnitTests.Entities;

public sealed class CartItemTests
{
    private static Guid ValidId => Guid.NewGuid();
    private static Money ValidPrice => Money.Create(10m, "USD");

    [Fact]
    public void Constructor_ValidInputs_CreatesCartItem()
    {
        var id = ValidId;
        var cartId = ValidId;
        var productId = ValidId;
        var item = new CartItem(id, cartId, productId, 2, ValidPrice);
        item.Id.Should().Be(id);
        item.CartId.Should().Be(cartId);
        item.ProductId.Should().Be(productId);
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(ValidPrice);
    }

    [Fact]
    public void Constructor_EmptyId_Throws()
    {
        var a = () => new CartItem(Guid.Empty, ValidId, ValidId, 1, ValidPrice);
        a.Should().Throw<ArgumentException>().WithMessage("*item ID cannot be empty*");
    }

    [Fact]
    public void Constructor_EmptyCartId_Throws()
    {
        var a = () => new CartItem(ValidId, Guid.Empty, ValidId, 1, ValidPrice);
        a.Should().Throw<ArgumentException>().WithMessage("*Cart ID cannot be empty*");
    }

    [Fact]
    public void Constructor_EmptyProductId_Throws()
    {
        var a = () => new CartItem(ValidId, ValidId, Guid.Empty, 1, ValidPrice);
        a.Should().Throw<ArgumentException>().WithMessage("*Product ID cannot be empty*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_InvalidQuantity_ThrowsInvalidCartQuantityException(int qty)
    {
        var a = () => new CartItem(ValidId, ValidId, ValidId, qty, ValidPrice);
        a.Should().Throw<InvalidCartQuantityException>();
    }

    [Fact]
    public void Constructor_NullUnitPrice_Throws()
    {
        var a = () => new CartItem(ValidId, ValidId, ValidId, 1, null!);
        a.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ZeroUnitPrice_ThrowsInvalidPriceException()
    {
        var a = () => new CartItem(ValidId, ValidId, ValidId, 1, Money.Zero());
        a.Should().Throw<InvalidPriceException>();
    }

    [Fact]
    public void ChangeQuantity_Updates()
    {
        var item = new CartItem(ValidId, ValidId, ValidId, 1, ValidPrice);
        item.ChangeQuantity(5);
        item.Quantity.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void ChangeQuantity_Invalid_ThrowsInvalidCartQuantityException(int qty)
    {
        var item = new CartItem(ValidId, ValidId, ValidId, 1, ValidPrice);
        var a = () => item.ChangeQuantity(qty);
        a.Should().Throw<InvalidCartQuantityException>();
    }

    [Fact]
    public void CalculateLineTotal_ReturnsProduct()
    {
        var item = new CartItem(ValidId, ValidId, ValidId, 3, Money.Create(9.99m, "USD"));
        var total = item.CalculateLineTotal();
        total.Amount.Should().Be(29.97m);
    }
}
