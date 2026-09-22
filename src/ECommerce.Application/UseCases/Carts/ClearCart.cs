using ECommerce.Application.DTOs.Carts;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Carts;

public record ClearCartCommand : IRequest<CartResponse>;

public sealed class ClearCartCommandValidator : AbstractValidator<ClearCartCommand>
{
    public ClearCartCommandValidator()
    {
    }
}

public sealed class ClearCartHandler : IRequestHandler<ClearCartCommand, CartResponse>
{
    private readonly ICartRepository _carts;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ClearCartHandler(ICartRepository carts, ICurrentUserService currentUser, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _carts = carts;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CartResponse> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.GetUserIdOrThrow();

        var cart = await _carts.GetByCustomerIdWithItemsAsync(customerId, cancellationToken);
        if (cart is null)
        {
            throw new NotFoundException<Cart>("No active cart found for current user.");
        }

        cart.Clear();
        _carts.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.From(cart).AdaptToType<CartResponse>();
    }
}
