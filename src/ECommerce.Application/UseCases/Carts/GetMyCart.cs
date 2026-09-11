using ECommerce.Application.DTOs.Carts;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Carts;

public record GetMyCartQuery : IRequest<CartResponse>;

public sealed class GetMyCartQueryValidator
    : AbstractValidator<GetMyCartQuery>
{
    public GetMyCartQueryValidator()
    {
    }
}

public sealed class GetMyCartHandler
    : IRequestHandler<GetMyCartQuery, CartResponse>
{
    private readonly ICartRepository _carts;
    private readonly ICurrentUserService _currentUser;
    private readonly IProductRepository _products;
    private readonly IMapper _mapper;

    public GetMyCartHandler(
        ICartRepository carts,
        ICurrentUserService currentUser,
        IProductRepository products,
        IMapper mapper)
    {
        _carts = carts;
        _currentUser = currentUser;
        _products = products;
        _mapper = mapper;
    }

    public async Task<CartResponse> Handle(GetMyCartQuery request,
        CancellationToken cancellationToken)
    {
        var customerId = _currentUser.GetUserIdOrThrow();

        var cart = await _carts.GetByCustomerIdWithItemsAsync(customerId, cancellationToken);

        if (cart is null)
        {
            cart = new Cart(Guid.NewGuid(), customerId);
            await _carts.AddAsync(cart, cancellationToken);
            return _mapper.From(cart).AdaptToType<CartResponse>();
        }

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
