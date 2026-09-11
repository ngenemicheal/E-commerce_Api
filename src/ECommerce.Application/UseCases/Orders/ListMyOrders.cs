using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Orders;

public record ListMyOrdersQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResponse<OrderResponse>>;

public sealed class ListMyOrdersQueryValidator
    : AbstractValidator<ListMyOrdersQuery>
{
    public ListMyOrdersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class ListMyOrdersHandler
    : IRequestHandler<ListMyOrdersQuery, PagedResponse<OrderResponse>>
{
    private readonly IOrderRepository _orders;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public ListMyOrdersHandler(
        IOrderRepository orders,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _orders = orders;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<PagedResponse<OrderResponse>> Handle(ListMyOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var customerId = _currentUser.GetUserIdOrThrow();

        var (items, totalCount) = await _orders.ListByCustomerIdAsync(
            customerId, request.Page, request.PageSize, cancellationToken);

        var mapped = _mapper.From(items).AdaptToType<List<OrderResponse>>();

        return new PagedResponse<OrderResponse>(
            mapped, totalCount, request.Page, request.PageSize);
    }
}
