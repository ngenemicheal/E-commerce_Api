using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Orders;

public record CheckoutCommand(Guid? CartId = null) : IRequest<OrderResponse>;

public sealed class CheckoutCommandValidator
    : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
    }
}

public sealed class CheckoutHandler
    : IRequestHandler<CheckoutCommand, OrderResponse>
{
    private readonly IOrderRepository _orders;
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _time;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CheckoutHandler(
        IOrderRepository orders,
        ICartRepository carts,
        IProductRepository products,
        ICurrentUserService currentUser,
        IDateTimeProvider time,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _orders = orders;
        _carts = carts;
        _products = products;
        _currentUser = currentUser;
        _time = time;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OrderResponse> Handle(CheckoutCommand request,
        CancellationToken cancellationToken)
    {
        var customerId = _currentUser.GetUserIdOrThrow();

        Cart? cart;
        if (request.CartId.HasValue && request.CartId.Value != Guid.Empty)
        {
            cart = await _carts.GetByIdWithItemsAsync(request.CartId.Value, cancellationToken);
            if (cart is null || cart.CustomerId != customerId)
            {
                throw new NotFoundException<Cart>(request.CartId.Value);
            }
        }
        else
        {
            cart = await _carts.GetByCustomerIdWithItemsAsync(customerId, cancellationToken);
            if (cart is null)
            {
                throw new NotFoundException<Cart>("No active cart found for current user.");
            }
        }

        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var productMap = productIds.Count > 0
            ? await _products.GetByIdsAsync(productIds, cancellationToken)
            : new Dictionary<Guid, Product>();

        foreach (var cartItem in cart.Items)
        {
            if (!productMap.TryGetValue(cartItem.ProductId, out var product))
            {
                throw new NotFoundException<Product>(cartItem.ProductId);
            }

            if (product.StockQuantity < cartItem.Quantity)
            {
                throw new ConflictException(
                    $"Insufficient stock for '{product.Name}' during checkout: " +
                    $"requested {cartItem.Quantity}, available {product.StockQuantity}.");
            }
        }

        var order = Order.CreateFromCart(Guid.NewGuid(), customerId, cart, _time.UtcNowOffset);

        foreach (var orderItem in order.Items)
        {
            if (productMap.TryGetValue(orderItem.ProductId, out var product))
            {
                product.DecreaseStock(orderItem.Quantity);
                _products.Update(product);
            }
        }

        cart.Clear();
        _carts.Update(cart);

        await _orders.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.From(order).AdaptToType<OrderResponse>();
    }
}
