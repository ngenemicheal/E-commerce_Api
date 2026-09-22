using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ECommerce.Infrastructure.Persistence.MongoDb.Repositories;

public class MongoProductRepository(IMongoDatabase db) : IProductRepository
{
    private readonly IMongoCollection<Product> _col = db.GetCollection<Product>("products");
    private readonly IMongoCollection<Category> _cats = db.GetCollection<Category>("categories");

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _col.Find(p => p.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (product is not null)
        {
            await HydrateCategoryAsync(new[] { product }, cancellationToken);
        }
        return product;
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var product = await _col.Find(p => p.Slug == normalized).FirstOrDefaultAsync(cancellationToken);
        if (product is not null)
        {
            await HydrateCategoryAsync(new[] { product }, cancellationToken);
        }
        return product;
    }

    public async Task<(List<Product> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? categoryId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Product>.Filter.Empty;
        if (categoryId.HasValue)
        {
            filter = Builders<Product>.Filter.Eq(p => p.CategoryId, categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            var name = Builders<Product>.Filter.Where(p => p.Name.ToLower().Contains(term));
            var desc = Builders<Product>.Filter.Where(p => p.Description != null && p.Description.ToLower().Contains(term));
            var searchFilter = Builders<Product>.Filter.Or(name, desc);
            filter = filter == Builders<Product>.Filter.Empty ? searchFilter : Builders<Product>.Filter.And(filter, searchFilter);
        }

        var totalCount = (int)await _col.CountDocumentsAsync(filter, null, cancellationToken);

        var items = await _col.Find(filter)
            .SortBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        await HydrateCategoryAsync(items, cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _col.InsertOneAsync(product, null, cancellationToken);
    }

    public void Update(Product product)
    {
        _col.ReplaceOne(p => p.Id == product.Id, product, new ReplaceOptions { IsUpsert = false });
    }

    public void Delete(Product product)
    {
        _col.DeleteOne(p => p.Id == product.Id);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var filter = Builders<Product>.Filter.Eq(p => p.Slug, normalized);
        if (excludeId.HasValue)
        {
            filter = Builders<Product>.Filter.And(filter,
                Builders<Product>.Filter.Ne(p => p.Id, excludeId.Value));
        }
        return await _col.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, Product>();
        }

        var items = await _col.Find(Builders<Product>.Filter.In(p => p.Id, idList))
            .ToListAsync(cancellationToken);

        await HydrateCategoryAsync(items, cancellationToken);

        return items.ToDictionary(p => p.Id);
    }

    private async Task HydrateCategoryAsync(IEnumerable<Product> products, CancellationToken ct)
    {
        var list = products.ToList();
        if (list.Count == 0)
        {
            return;
        }

        var categoryIds = list.Select(p => p.CategoryId).Where(id => id != Guid.Empty).Distinct().ToList();
        if (categoryIds.Count == 0)
        {
            return;
        }

        var categories = await _cats.Find(Builders<Category>.Filter.In(c => c.Id, categoryIds))
            .ToListAsync(ct);

        var map = categories.ToDictionary(c => c.Id);

        foreach (var p in list)
        {
            if (map.TryGetValue(p.CategoryId, out var cat))
            {
                typeof(Product).GetProperty("Category", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                    ?.SetValue(p, cat);
            }
        }
    }
}
