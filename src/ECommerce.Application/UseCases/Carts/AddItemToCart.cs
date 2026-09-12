using ECommerce.Application.DTOs.Carts;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.ValueObjects;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Carts;

public record AddItemToCartCommand(Guid ProductId, int Quantity) : IRequest<CartResponse>;

public sealed class AddItemToCartCommandValidator : AbstractValidator<AddItemToCartCommand>
{
    public AddItemToCartCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Quantity must be at least 1.");
    }
}

public sealed class AddItemToCartHandler : IRequestHandler<AddItemToCartCommand, CartResponse>
{
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _products2;
    private readonly IMapper _mapper;

    public AddItemToCartHandler(ICartRepository carts, IProductRepository products, ICurrentUserService currentUser, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _carts = carts;
        _products = products;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _products2 = products;
        _mapper = mapper;
    }

    public async Task<CartResponse> Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.GetUserIdOrThrow();

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

        var cart = await _carts.GetByCustomerIdWithItemsAsync(customerId, cancellationToken);
        cart ??= new Cart(Guid.NewGuid(), customerId);

        if (cart.Id == Guid.Empty)
        {
            await _carts.AddAsync(cart, cancellationToken);
        }

        cart.AddItem(request.ProductId, request.Quantity,
            Money.Create(product.Price.Amount, product.Price.Currency));

        if (cart.Id != Guid.Empty)
        {
            _carts.Update(cart);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        if (productIds.Count > 0)
        {
            var productMap = await _products2.GetByIdsAsync(productIds, cancellationToken);
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
