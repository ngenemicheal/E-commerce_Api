using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Orders;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderResponse>;

public sealed class GetOrderByIdQueryValidator
    : AbstractValidator<GetOrderByIdQuery>
{
    public GetOrderByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetOrderByIdHandler
    : IRequestHandler<GetOrderByIdQuery, OrderResponse>
{
    private readonly IOrderRepository _orders;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;
    private readonly IMapper _mapper;

    public GetOrderByIdHandler(
        IOrderRepository orders,
        ICurrentUserService currentUser,
        IIdentityService identity,
        IMapper mapper)
    {
        _orders = orders;
        _currentUser = currentUser;
        _identity = identity;
        _mapper = mapper;
    }

    public async Task<OrderResponse> Handle(GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException<Order>(request.Id);
        }

        var userId = _currentUser.UserId;
        var isAdmin = userId.HasValue &&
                      await _identity.IsUserInRoleAsync(userId.Value, "Admin", cancellationToken);

        if (!isAdmin && order.CustomerId != userId)
        {
            throw new ForbiddenException("You are not allowed to view this order.");
        }

        return _mapper.From(order).AdaptToType<OrderResponse>();
    }
}
