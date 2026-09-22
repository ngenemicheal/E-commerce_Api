using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Orders;

public record CancelOrderCommand(Guid OrderId) : IRequest<OrderResponse>;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

public sealed class CancelOrderHandler : IRequestHandler<CancelOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CancelOrderHandler(
        IOrderRepository orders,
        IProductRepository products,
        ICurrentUserService currentUser,
        IIdentityService identity,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _orders = orders;
        _products = products;
        _currentUser = currentUser;
        _identity = identity;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OrderResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithItemsAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException<Order>(request.OrderId);
        }

        var userId = _currentUser.GetUserIdOrThrow();
        var isAdmin = await _identity.IsUserInRoleAsync(userId, "Admin", cancellationToken);

        if (!isAdmin && order.CustomerId != userId)
        {
            throw new ForbiddenException("You are not allowed to cancel this order.");
        }

        order.Cancel();

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var productMap = productIds.Count > 0
            ? await _products.GetByIdsAsync(productIds, cancellationToken)
            : new Dictionary<Guid, Product>();

        foreach (var item in order.Items)
        {
            if (productMap.TryGetValue(item.ProductId, out var product))
            {
                product.IncreaseStock(item.Quantity);
                _products.Update(product);
            }
        }

        _orders.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.From(order).AdaptToType<OrderResponse>();
    }
}
