using ECommerce.Application.DTOs.Carts;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.UseCases.Carts;
using ECommerce.Application.UseCases.Orders;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.ValueObjects;
using MapsterMapper;
using MediatR;
using NSubstitute;

namespace ECommerce.Application.UnitTests.UseCases;

public sealed class CartAndOrderHandlerTests
{
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;
    private readonly IOrderRepository _orders;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;
    private readonly IDateTimeProvider _time;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly Guid _customerId;

    public CartAndOrderHandlerTests()
    {
        _carts = Substitute.For<ICartRepository>();
        _products = Substitute.For<IProductRepository>();
        _orders = Substitute.For<IOrderRepository>();
        _customerId = Guid.NewGuid();
        _currentUser = TestHelper.CreateCurrentUser(_customerId, email: "customer@test.local", roles: "Customer");
        _identity = Substitute.For<IIdentityService>();
        _time = TestHelper.CreateDateTimeProvider();
        _uow = Substitute.For<IUnitOfWork>();
        _mapper = TestHelper.CreateMapper();
    }

    private static void SetCategoryNav(Product p, Category c)
        => p.GetType().GetProperty("Category")?.SetValue(p, c);

    #region AddItemToCart Tests

    [Fact]
    public async Task AddItemToCart_NewCart_CreatesCartAndAddsItem()
    {
        var categoryId = Guid.NewGuid();
        var cat = new Category(categoryId, "Books", "books");
        var productId = Guid.NewGuid();
        var product = new Product(productId, "Book", "book", Money.Create(15m, "USD"), categoryId, 10);
        SetCategoryNav(product, cat);

        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);
        var newCalls = 0;
        _carts.GetByCustomerIdWithItemsAsync(_customerId, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                newCalls++;
                if (newCalls == 1) return (Cart?)null;
                return new Cart(Guid.NewGuid(), _customerId);
            });

        var cmd = new AddItemToCartCommand(productId, 2);
        var handler = new AddItemToCartHandler(_carts, _products, _currentUser, _uow, _mapper);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        await _carts.Received(1).AddAsync(Arg.Is<Cart>(c => c.CustomerId == _customerId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItemToCart_ExistingCart_AddsItem()
    {
        var categoryId = Guid.NewGuid();
        var cat = new Category(categoryId, "Books", "books");
        var productId = Guid.NewGuid();
        var product = new Product(productId, "Book", "book", Money.Create(15m, "USD"), categoryId, 10);
        SetCategoryNav(product, cat);

        var existingCart = new Cart(Guid.NewGuid(), _customerId);

        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);
        _carts.GetByCustomerIdWithItemsAsync(_customerId, Arg.Any<CancellationToken>())
            .Returns(_ => existingCart, _ => existingCart, _ => existingCart);

        var cmd = new AddItemToCartCommand(productId, 2);
        var handler = new AddItemToCartHandler(_carts, _products, _currentUser, _uow, _mapper);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        existingCart.Items.Should().ContainSingle(i => i.ProductId == productId && i.Quantity == 2);
    }

    [Fact]
    public async Task AddItemToCart_MissingProduct_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var cmd = new AddItemToCartCommand(productId, 1);
        var handler = new AddItemToCartHandler(_carts, _products, _currentUser, _uow, _mapper);
        var action = () => handler.Handle(cmd, CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException<Product>>();
    }

    [Fact]
    public async Task AddItemToCart_InsufficientStock_ThrowsConflict()
    {
        var categoryId = Guid.NewGuid();
        var cat = new Category(categoryId, "Books", "books");
        var productId = Guid.NewGuid();
        var product = new Product(productId, "Book", "book", Money.Create(15m, "USD"), categoryId, 2);
        SetCategoryNav(product, cat);
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var cmd = new AddItemToCartCommand(productId, 5);
        var handler = new AddItemToCartHandler(_carts, _products, _currentUser, _uow, _mapper);
        var action = () => handler.Handle(cmd, CancellationToken.None);
        await action.Should().ThrowAsync<ConflictException>();
    }

    #endregion

    #region Checkout Tests

    [Fact]
    public async Task Checkout_ValidCart_CreatesOrderAndDecreasesStockAndClearsCart()
    {
        var categoryId = Guid.NewGuid();
        var cat = new Category(categoryId, "Books", "books");

        var productId1 = Guid.NewGuid();
        var product1 = new Product(productId1, "Book A", "book-a", Money.Create(10m, "USD"), categoryId, 5);
        SetCategoryNav(product1, cat);

        var productId2 = Guid.NewGuid();
        var product2 = new Product(productId2, "Book B", "book-b", Money.Create(20m, "USD"), categoryId, 3);
        SetCategoryNav(product2, cat);

        var cartId = Guid.NewGuid();
        var cart = new Cart(cartId, _customerId);
        cart.AddItem(productId1, 2, Money.Create(10m, "USD"));
        cart.AddItem(productId2, 1, Money.Create(20m, "USD"));

        _carts.GetByCustomerIdWithItemsAsync(_customerId, Arg.Any<CancellationToken>()).Returns(cart);
        _products.GetByIdsAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Count() == 2), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [productId1] = product1,
                [productId2] = product2,
            });

        var handler = new CheckoutHandler(_orders, _carts, _products, _currentUser, _time, _uow, _mapper);
        var result = await handler.Handle(new CheckoutCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Pending);
        result.TotalAmount.Amount.Should().Be(40m);
        result.Items.Count.Should().Be(2);

        product1.StockQuantity.Should().Be(3);
        product2.StockQuantity.Should().Be(2);

        _products.Received(2).Update(Arg.Any<Product>());
        _carts.Received(1).Update(cart);
        cart.IsEmpty.Should().BeTrue();
        await _orders.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Checkout_NoCart_ThrowsNotFoundException()
    {
        _carts.GetByCustomerIdWithItemsAsync(_customerId, Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var handler = new CheckoutHandler(_orders, _carts, _products, _currentUser, _time, _uow, _mapper);
        var action = () => handler.Handle(new CheckoutCommand(), CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException<Cart>>();
    }

    [Fact]
    public async Task Checkout_ProductMissing_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        var cart = new Cart(Guid.NewGuid(), _customerId);
        cart.AddItem(productId, 1, Money.Create(10m, "USD"));

        _carts.GetByCustomerIdWithItemsAsync(_customerId, Arg.Any<CancellationToken>()).Returns(cart);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>());

        var handler = new CheckoutHandler(_orders, _carts, _products, _currentUser, _time, _uow, _mapper);
        var action = () => handler.Handle(new CheckoutCommand(), CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException<Product>>();
    }

    [Fact]
    public async Task Checkout_InsufficientStock_ThrowsConflict()
    {
        var categoryId = Guid.NewGuid();
        var cat = new Category(categoryId, "Books", "books");
        var productId = Guid.NewGuid();
        var product = new Product(productId, "Book", "book", Money.Create(10m, "USD"), categoryId, 1);
        SetCategoryNav(product, cat);

        var cart = new Cart(Guid.NewGuid(), _customerId);
        cart.AddItem(productId, 3, Money.Create(10m, "USD"));

        _carts.GetByCustomerIdWithItemsAsync(_customerId, Arg.Any<CancellationToken>()).Returns(cart);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [productId] = product });

        var handler = new CheckoutHandler(_orders, _carts, _products, _currentUser, _time, _uow, _mapper);
        var action = () => handler.Handle(new CheckoutCommand(), CancellationToken.None);
        await action.Should().ThrowAsync<ConflictException>().WithMessage("*Insufficient stock*");
    }

    #endregion

    #region CancelOrder Tests

    [Fact]
    public async Task CancelOrder_CustomerOwnsOrder_CancelsAndRestoresStock()
    {
        var categoryId = Guid.NewGuid();
        var cat = new Category(categoryId, "Books", "books");

        var productId1 = Guid.NewGuid();
        var product1 = new Product(productId1, "Book A", "book-a", Money.Create(10m, "USD"), categoryId, 0);
        SetCategoryNav(product1, cat);

        var productId2 = Guid.NewGuid();
        var product2 = new Product(productId2, "Book B", "book-b", Money.Create(20m, "USD"), categoryId, 0);
        SetCategoryNav(product2, cat);

        var cart = new Cart(Guid.NewGuid(), _customerId);
        cart.AddItem(productId1, 2, Money.Create(10m, "USD"));
        cart.AddItem(productId2, 1, Money.Create(20m, "USD"));
        var order = Order.CreateFromCart(Guid.NewGuid(), _customerId, cart, _time.UtcNowOffset);

        _orders.GetByIdWithItemsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _identity.IsUserInRoleAsync(_customerId, "Admin", Arg.Any<CancellationToken>()).Returns(false);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [productId1] = product1,
                [productId2] = product2,
            });

        var handler = new CancelOrderHandler(_orders, _products, _currentUser, _identity, _uow, _mapper);
        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Cancelled);
        product1.StockQuantity.Should().Be(2);
        product2.StockQuantity.Should().Be(1);
        _orders.Received(1).Update(order);
    }

    [Fact]
    public async Task CancelOrder_WrongCustomer_ThrowsForbidden()
    {
        var otherUserId = Guid.NewGuid();
        var cart = new Cart(Guid.NewGuid(), otherUserId);
        cart.AddItem(Guid.NewGuid(), 1, Money.Create(10m, "USD"));
        var order = Order.CreateFromCart(Guid.NewGuid(), otherUserId, cart, _time.UtcNowOffset);

        _orders.GetByIdWithItemsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _identity.IsUserInRoleAsync(_customerId, "Admin", Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CancelOrderHandler(_orders, _products, _currentUser, _identity, _uow, _mapper);
        var action = () => handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);
        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CancelOrder_Admin_CancelsAnyOrder()
    {
        var otherUserId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var cat = new Category(categoryId, "Books", "books");
        var product = new Product(productId, "Book", "book", Money.Create(10m, "USD"), categoryId, 0);
        SetCategoryNav(product, cat);

        var cart = new Cart(Guid.NewGuid(), otherUserId);
        cart.AddItem(productId, 2, Money.Create(10m, "USD"));
        var order = Order.CreateFromCart(Guid.NewGuid(), otherUserId, cart, _time.UtcNowOffset);

        _orders.GetByIdWithItemsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _identity.IsUserInRoleAsync(_customerId, "Admin", Arg.Any<CancellationToken>()).Returns(true);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [productId] = product });

        var handler = new CancelOrderHandler(_orders, _products, _currentUser, _identity, _uow, _mapper);
        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task CancelOrder_OrderNotFound_ThrowsNotFoundException()
    {
        var orderId = Guid.NewGuid();
        _orders.GetByIdWithItemsAsync(orderId, Arg.Any<CancellationToken>()).Returns((Order?)null);
        var handler = new CancelOrderHandler(_orders, _products, _currentUser, _identity, _uow, _mapper);
        var action = () => handler.Handle(new CancelOrderCommand(orderId), CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException<Order>>();
    }

    #endregion
}
