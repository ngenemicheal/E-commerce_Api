using System.Reflection;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MongoDB.Driver;

namespace ECommerce.Infrastructure.Persistence.MongoDb.Repositories;

public class MongoOrderRepository(IMongoDatabase db) : IOrderRepository
{
    private readonly IMongoCollection<Order> _col = db.GetCollection<Order>("orders");

    private static readonly FieldInfo ItemsField =
        typeof(Order).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public async Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _col.Find(o => o.Id == id).FirstOrDefaultAsync(cancellationToken);
        return order;
    }

    public async Task<(List<Order> Items, int TotalCount)> ListByCustomerIdAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.CustomerId, customerId);
        var totalCount = (int)await _col.CountDocumentsAsync(filter, null, cancellationToken);
        var items = await _col.Find(filter)
            .SortByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<(List<Order> Items, int TotalCount)> ListAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = (int)await _col.CountDocumentsAsync(_ => true, null, cancellationToken);
        var items = await _col.Find(_ => true)
            .SortByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _col.InsertOneAsync(order, null, cancellationToken);
    }

    public void Update(Order order)
    {
        _col.ReplaceOne(o => o.Id == order.Id, order, new ReplaceOptions { IsUpsert = false });
    }
}
