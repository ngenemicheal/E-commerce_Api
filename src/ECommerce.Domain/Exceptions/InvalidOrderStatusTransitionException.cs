using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Exceptions;

public class InvalidOrderStatusTransitionException : DomainException
{
    public OrderStatus From { get; }
    public OrderStatus To { get; }

    public InvalidOrderStatusTransitionException(OrderStatus from, OrderStatus to)
        : base($"Invalid order status transition: {from} -> {to}.")
    {
        From = from;
        To = to;
    }
}
