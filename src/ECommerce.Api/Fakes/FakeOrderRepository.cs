using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Api.Fakes;

public class FakeOrderRepository : IOrderRepository
{
    private static readonly Dictionary<Guid, Order> _byId = new();
    private static readonly Dictionary<Guid, List<Guid>> _customerToOrderIds = new();

    public Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var order);
        return Task.FromResult<Order?>(order);
    }

    public Task<(List<Order> Items, int TotalCount)> ListByCustomerIdAsync(
        Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!_customerToOrderIds.TryGetValue(customerId, out var ids))
            return Task.FromResult((new List<Order>(), 0));

        var orders = ids
            .Select(id => _byId[id])
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var total = orders.Count;
        var items = orders
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((items, total));
    }

    public Task<(List<Order> Items, int TotalCount)> ListAllAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var orders = _byId.Values
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var total = orders.Count;
        var items = orders
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((items, total));
    }

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (_byId.ContainsKey(order.Id))
            throw new InvalidOperationException($"Order with ID {order.Id} already exists.");
        _byId[order.Id] = order;

        if (!_customerToOrderIds.TryGetValue(order.CustomerId, out var list))
        {
            list = new List<Guid>();
            _customerToOrderIds[order.CustomerId] = list;
        }
        list.Add(order.Id);
        return Task.CompletedTask;
    }

    public void Update(Order order)
    {
        if (!_byId.ContainsKey(order.Id))
            throw new InvalidOperationException($"Order with ID {order.Id} not found.");
        _byId[order.Id] = order;
    }
}
