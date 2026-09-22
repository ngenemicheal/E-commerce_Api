using ECommerce.Application.DTOs.Carts;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Carts;

public record UpdateCartItemQuantityCommand(Guid ProductId, int Quantity) : IRequest<CartResponse>;

public sealed class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Quantity cannot be negative.");
    }
}

public sealed class UpdateCartItemQuantityHandler : IRequestHandler<UpdateCartItemQuantityCommand, CartResponse>
{
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateCartItemQuantityHandler(ICartRepository carts, IProductRepository products, ICurrentUserService currentUser, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _carts = carts;
        _products = products;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CartResponse> Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.GetUserIdOrThrow();

        var cart = await _carts.GetByCustomerIdWithItemsAsync(customerId, cancellationToken);
        if (cart is null)
        {
            throw new NotFoundException<Cart>("No active cart found for current user.");
        }

        if (request.Quantity > 0)
        {
            var product = await _products.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                throw new NotFoundException<Product>(request.ProductId);
            }

            if (product.StockQuantity < request.Quantity)
            {
                throw new ConflictException(
                    $"Insufficient stock for product '{product.Name}'. " +
                    $"Requested {request.Quantity}, available {product.StockQuantity}.");
            }
        }

        cart.UpdateQuantity(request.ProductId, request.Quantity);
        _carts.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        if (productIds.Count > 0)
        {
            var productMap = await _products.GetByIdsAsync(productIds, cancellationToken);
            foreach (var item in cart.Items)
            {
                if (productMap.TryGetValue(item.ProductId, out var p))
                {
                    typeof(CartItem).GetProperty("Product")?.SetValue(item, p, null);
                }
            }
        }

        return _mapper.From(cart).AdaptToType<CartResponse>();
    }
}
