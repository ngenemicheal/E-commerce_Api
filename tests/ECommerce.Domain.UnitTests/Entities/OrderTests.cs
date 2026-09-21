using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.UnitTests.Entities;

public sealed class OrderTests
{
    private static Guid ValidId => Guid.NewGuid();
    private static Guid ValidCustomerId => Guid.NewGuid();
    private static Money ValidPrice => Money.Create(10m, "USD");

    private static Cart BuildPopulatedCart(Guid customerId)
    {
        var cartId = ValidId;
        var cart = new Cart(cartId, customerId);
        cart.AddItem(ValidId, 2, ValidPrice);
        cart.AddItem(Guid.NewGuid(), 1, Money.Create(20m, "USD"));
        return cart;
    }

    private static Cart BuildEmptyCart(Guid customerId)
    {
        return new Cart(ValidId, customerId);
    }

    private static Order CreateValidOrder(out Guid customerId, out DateTimeOffset createdAt)
    {
        customerId = ValidCustomerId;
        createdAt = DateTimeOffset.UtcNow;
        var cart = BuildPopulatedCart(customerId);
        return Order.CreateFromCart(ValidId, customerId, cart, createdAt);
    }

    private static Order CreateValidOrder()
    {
        return CreateValidOrder(out _, out _);
    }

    [Fact]
    public void CreateFromCart_Valid_CreatesOrder()
    {
        var orderId = ValidId;
        var customerId = ValidCustomerId;
        var cart = BuildPopulatedCart(customerId);
        var now = DateTimeOffset.UtcNow;

        var order = Order.CreateFromCart(orderId, customerId, cart, now);

        order.Id.Should().Be(orderId);
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.CreatedAt.Should().Be(now);
        order.TotalAmount.Amount.Should().Be(40m);
        order.Items.Count.Should().Be(2);
    }

    [Fact]
    public void CreateFromCart_EmptyOrderId_Throws()
    {
        var customerId = ValidCustomerId;
        var a = () => Order.CreateFromCart(Guid.Empty, customerId, BuildPopulatedCart(customerId), DateTimeOffset.UtcNow);
        a.Should().Throw<ArgumentException>().WithMessage("*Order ID cannot be empty*");
    }

    [Fact]
    public void CreateFromCart_EmptyCustomerId_Throws()
    {
        var a = () => Order.CreateFromCart(ValidId, Guid.Empty, BuildPopulatedCart(Guid.Empty), DateTimeOffset.UtcNow);
        a.Should().Throw<ArgumentException>().WithMessage("*Customer ID cannot be empty*");
    }

    [Fact]
    public void CreateFromCart_NullCart_Throws()
    {
        var a = () => Order.CreateFromCart(ValidId, ValidCustomerId, null!, DateTimeOffset.UtcNow);
        a.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateFromCart_WrongCustomer_ThrowsInvalidOperation()
    {
        var cart = BuildPopulatedCart(ValidCustomerId);
        var a = () => Order.CreateFromCart(ValidId, Guid.NewGuid(), cart, DateTimeOffset.UtcNow);
        a.Should().Throw<InvalidOperationException>().WithMessage("*does not belong to customer*");
    }

    [Fact]
    public void CreateFromCart_EmptyCart_ThrowsEmptyCartOnCheckoutException()
    {
        var customerId = ValidCustomerId;
        var cart = BuildEmptyCart(customerId);
        var a = () => Order.CreateFromCart(ValidId, customerId, cart, DateTimeOffset.UtcNow);
        a.Should().Throw<EmptyCartOnCheckoutException>();
    }

    [Fact]
    public void SetPaid_FromPending_SetsPaid()
    {
        var order = CreateValidOrder();
        var now = DateTimeOffset.UtcNow;
        order.SetPaid(now);
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().Be(now);
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void SetPaid_NotPending_ThrowsInvalidTransition(OrderStatus status)
    {
        var order = CreateValidOrder();
        if (status == OrderStatus.Paid) order.SetPaid(DateTimeOffset.UtcNow);
        else if (status == OrderStatus.Shipped) { order.SetPaid(DateTimeOffset.UtcNow); order.SetShipped(DateTimeOffset.UtcNow); }
        else if (status == OrderStatus.Delivered) { order.SetPaid(DateTimeOffset.UtcNow); order.SetShipped(DateTimeOffset.UtcNow); order.SetDelivered(DateTimeOffset.UtcNow); }
        else if (status == OrderStatus.Cancelled) order.Cancel();

        var a = () => order.SetPaid(DateTimeOffset.UtcNow);
        a.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void SetShipped_FromPaid_SetsShipped()
    {
        var order = CreateValidOrder();
        order.SetPaid(DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        order.SetShipped(now);
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().Be(now);
    }

    [Fact]
    public void SetShipped_NotPaid_Throws()
    {
        var order = CreateValidOrder();
        var a = () => order.SetShipped(DateTimeOffset.UtcNow);
        a.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void SetDelivered_FromShipped_SetsDelivered()
    {
        var order = CreateValidOrder();
        order.SetPaid(DateTimeOffset.UtcNow);
        order.SetShipped(DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        order.SetDelivered(now);
        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().Be(now);
    }

    [Fact]
    public void SetDelivered_NotShipped_Throws()
    {
        var order = CreateValidOrder();
        order.SetPaid(DateTimeOffset.UtcNow);
        var a = () => order.SetDelivered(DateTimeOffset.UtcNow);
        a.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void Cancel_FromPending_SetsCancelled()
    {
        var order = CreateValidOrder();
        order.Cancel();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromPaid_SetsCancelled()
    {
        var order = CreateValidOrder();
        order.SetPaid(DateTimeOffset.UtcNow);
        order.Cancel();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public void Cancel_NotPendingOrPaid_Throws(OrderStatus status)
    {
        var order = CreateValidOrder();
        order.SetPaid(DateTimeOffset.UtcNow);
        if (status == OrderStatus.Shipped) order.SetShipped(DateTimeOffset.UtcNow);
        else { order.SetShipped(DateTimeOffset.UtcNow); order.SetDelivered(DateTimeOffset.UtcNow); }

        var a = () => order.Cancel();
        a.Should().Throw<InvalidOrderStatusTransitionException>();
    }
}
