using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.UnitTests.Entities;

public sealed class CartTests
{
    private static Guid ValidId => Guid.NewGuid();
    private static Guid ValidCustomerId => Guid.NewGuid();
    private static Money ValidPrice => Money.Create(25m, "USD");

    [Fact]
    public void Constructor_ValidInputs_CreatesCart()
    {
        var id = ValidId;
        var customerId = ValidCustomerId;
        var cart = new Cart(id, customerId);
        cart.Id.Should().Be(id);
        cart.CustomerId.Should().Be(customerId);
        cart.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        cart.Items.Should().BeEmpty();
        cart.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Constructor_EmptyId_Throws()
    {
        var a = () => new Cart(Guid.Empty, ValidCustomerId);
        a.Should().Throw<ArgumentException>().WithMessage("*Cart ID cannot be empty*");
    }

    [Fact]
    public void Constructor_EmptyCustomerId_Throws()
    {
        var a = () => new Cart(ValidId, Guid.Empty);
        a.Should().Throw<ArgumentException>().WithMessage("*Customer ID cannot be empty*");
    }

    [Fact]
    public void AddItem_NewItem_AddsToList()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        var productId = ValidId;
        cart.AddItem(productId, 2, ValidPrice);
        cart.Items.Should().ContainSingle();
        cart.Items[0].ProductId.Should().Be(productId);
        cart.Items[0].Quantity.Should().Be(2);
        cart.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void AddItem_SameProduct_IncreasesQuantity()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        var productId = ValidId;
        cart.AddItem(productId, 2, ValidPrice);
        cart.AddItem(productId, 3, ValidPrice);
        cart.Items.Should().ContainSingle();
        cart.Items[0].Quantity.Should().Be(5);
    }

    [Fact]
    public void AddItem_EmptyProductId_Throws()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        var a = () => cart.AddItem(Guid.Empty, 1, ValidPrice);
        a.Should().Throw<ArgumentException>().WithMessage("*Product ID cannot be empty*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddItem_InvalidQuantity_ThrowsInvalidCartQuantityException(int qty)
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        var a = () => cart.AddItem(ValidId, qty, ValidPrice);
        a.Should().Throw<InvalidCartQuantityException>();
    }

    [Fact]
    public void AddItem_NullUnitPrice_Throws()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        var a = () => cart.AddItem(ValidId, 1, null!);
        a.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddItem_ZeroUnitPrice_ThrowsInvalidPriceException()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        var a = () => cart.AddItem(ValidId, 1, Money.Zero());
        a.Should().Throw<InvalidPriceException>();
    }

    [Fact]
    public void UpdateQuantity_ExistingItem_Updates()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        var productId = ValidId;
        cart.AddItem(productId, 2, ValidPrice);
        cart.UpdateQuantity(productId, 7);
        cart.Items[0].Quantity.Should().Be(7);
    }

    [Fact]
    public void UpdateQuantity_SetToZero_RemovesItem()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        var productId = ValidId;
        cart.AddItem(productId, 2, ValidPrice);
        cart.UpdateQuantity(productId, 0);
        cart.Items.Should().BeEmpty();
        cart.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void UpdateQuantity_Nonexistent_NoOp()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        cart.UpdateQuantity(ValidId, 5);
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void UpdateQuantity_Negative_Throws()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        var a = () => cart.UpdateQuantity(ValidId, -1);
        a.Should().Throw<InvalidCartQuantityException>();
    }

    [Fact]
    public void RemoveItem_Existing_Removes()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        var productId = ValidId;
        cart.AddItem(productId, 1, ValidPrice);
        cart.RemoveItem(productId);
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_Nonexistent_NoOp()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        cart.RemoveItem(ValidId);
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        cart.AddItem(ValidId, 1, ValidPrice);
        cart.AddItem(Guid.NewGuid(), 1, ValidPrice);
        cart.Clear();
        cart.Items.Should().BeEmpty();
        cart.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void CalculateTotal_Empty_ReturnsZero()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        var total = cart.CalculateTotal();
        total.Amount.Should().Be(0m);
    }

    [Fact]
    public void CalculateTotal_MultipleItems_ReturnsSum()
    {
        var cart = new Cart(ValidId, ValidCustomerId);
        cart.AddItem(ValidId, 2, Money.Create(10m, "USD"));
        cart.AddItem(Guid.NewGuid(), 1, Money.Create(5m, "USD"));
        var total = cart.CalculateTotal();
        total.Amount.Should().Be(25m);
    }
}
