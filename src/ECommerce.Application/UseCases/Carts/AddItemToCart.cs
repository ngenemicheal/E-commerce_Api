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
    private readonly IMapper _mapper;

    public AddItemToCartHandler(ICartRepository carts, IProductRepository products, ICurrentUserService currentUser, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _carts = carts;
        _products = products;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
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
        var isNew = cart is null;

        if (isNew)
        {
            cart = new Cart(Guid.NewGuid(), customerId);
        }

        cart!.AddItem(request.ProductId, request.Quantity,
            Money.Create(product.Price.Amount, product.Price.Currency));

        if (isNew)
        {
            await _carts.AddAsync(cart, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await _carts.GetByCustomerIdWithItemsAsync(customerId, cancellationToken);
        return _mapper.From(refreshed).AdaptToType<CartResponse>();
    }
}
