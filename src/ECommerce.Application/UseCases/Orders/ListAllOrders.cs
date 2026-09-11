using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Orders;

public record ListAllOrdersQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResponse<OrderResponse>>;

public sealed class ListAllOrdersQueryValidator
    : AbstractValidator<ListAllOrdersQuery>
{
    public ListAllOrdersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class ListAllOrdersHandler
    : IRequestHandler<ListAllOrdersQuery, PagedResponse<OrderResponse>>
{
    private readonly IOrderRepository _orders;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;
    private readonly IMapper _mapper;

    public ListAllOrdersHandler(
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

    public async Task<PagedResponse<OrderResponse>> Handle(ListAllOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserIdOrThrow();

        if (!await _identity.IsUserInRoleAsync(userId, "Admin", cancellationToken))
        {
            throw new ForbiddenException("Admin role is required to list all orders.");
        }

        var (items, totalCount) = await _orders.ListAllAsync(
            request.Page, request.PageSize, cancellationToken);

        var mapped = _mapper.From(items).AdaptToType<List<OrderResponse>>();

        return new PagedResponse<OrderResponse>(
            mapped, totalCount, request.Page, request.PageSize);
    }
}
