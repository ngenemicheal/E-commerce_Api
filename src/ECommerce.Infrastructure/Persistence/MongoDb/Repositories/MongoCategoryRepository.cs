using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MongoDB.Driver;

namespace ECommerce.Infrastructure.Persistence.MongoDb.Repositories;

public class MongoCategoryRepository(IMongoDatabase db) : ICategoryRepository
{
    private readonly IMongoCollection<Category> _col = db.GetCollection<Category>("categories");

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _col.Find(c => c.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Category>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _col.Find(_ => true).SortBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _col.InsertOneAsync(category, null, cancellationToken);
    }

    public void Update(Category category)
    {
        _col.ReplaceOne(c => c.Id == category.Id, category, new ReplaceOptions { IsUpsert = false });
    }

    public void Delete(Category category)
    {
        _col.DeleteOne(c => c.Id == category.Id);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var filter = Builders<Category>.Filter.Eq(c => c.Slug, normalized);
        if (excludeId.HasValue)
        {
            filter = Builders<Category>.Filter.And(filter,
                Builders<Category>.Filter.Ne(c => c.Id, excludeId.Value));
        }
        return await _col.Find(filter).AnyAsync(cancellationToken);
    }
}
