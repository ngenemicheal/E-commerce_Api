using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Api.Fakes;

public class FakeProductRepository : IProductRepository
{
    private static readonly Dictionary<Guid, Product> _store;

    static FakeProductRepository()
    {
        _store = new Dictionary<Guid, Product>();

        static Product P(Guid id, string name, string slug, decimal price, Guid categoryId, int stock, string? desc = null)
            => new(id, name, slug, Money.Create(price, "USD"), categoryId, stock, desc);

        _store.Add(new Guid("11111111-1111-1111-1111-1111111111a1"), P(
            new Guid("11111111-1111-1111-1111-1111111111a1"),
            "Smartphone Pro X", "smartphone-pro-x", 899.99m, FakeCategoryRepository.ElectronicsId, 50,
            "Flagship smartphone with 6.7\" OLED display, 512GB storage, and 108MP camera."));
        _store.Add(new Guid("11111111-1111-1111-1111-1111111111a2"), P(
            new Guid("11111111-1111-1111-1111-1111111111a2"),
            "Wireless Noise-Canceling Headphones", "wireless-nc-headphones", 249.99m, FakeCategoryRepository.ElectronicsId, 120,
            "Over-ear Bluetooth headphones with 40-hour battery and active noise cancellation."));
        _store.Add(new Guid("11111111-1111-1111-1111-1111111111a3"), P(
            new Guid("11111111-1111-1111-1111-1111111111a3"),
            "15\" Ultrabook Laptop", "ultrabook-15", 1299.00m, FakeCategoryRepository.ElectronicsId, 25,
            "Lightweight business laptop with 16GB RAM, 1TB SSD, and 12-hour battery life."));

        _store.Add(new Guid("11111111-1111-1111-1111-1111111111b1"), P(
            new Guid("11111111-1111-1111-1111-1111111111b1"),
            "Classic Cotton T-Shirt", "classic-cotton-tee", 24.99m, FakeCategoryRepository.ClothingId, 300,
            "100% organic cotton crew-neck tee, available in 8 colors."));
        _store.Add(new Guid("11111111-1111-1111-1111-1111111111b2"), P(
            new Guid("11111111-1111-1111-1111-1111111111b2"),
            "Slim Fit Denim Jeans", "slim-denim-jeans", 79.50m, FakeCategoryRepository.ClothingId, 180,
            "Stretch denim with a modern slim cut, mid-rise waist, and tapered leg."));
        _store.Add(new Guid("11111111-1111-1111-1111-1111111111b3"), P(
            new Guid("11111111-1111-1111-1111-1111111111b3"),
            "Running Sneakers", "running-sneakers", 119.00m, FakeCategoryRepository.ClothingId, 90,
            "Breathable mesh upper, responsive cushioning, durable rubber outsole."));

        _store.Add(new Guid("11111111-1111-1111-1111-1111111111c1"), P(
            new Guid("11111111-1111-1111-1111-1111111111c1"),
            "Clean Code: A Handbook of Agile Software Craftsmanship", "clean-code", 34.99m, FakeCategoryRepository.BooksId, 75,
            "Robert C. Martin's classic guide to writing maintainable, professional code."));
        _store.Add(new Guid("11111111-1111-1111-1111-1111111111c2"), P(
            new Guid("11111111-1111-1111-1111-1111111111c2"),
            "The Pragmatic Programmer", "pragmatic-programmer", 42.00m, FakeCategoryRepository.BooksId, 60,
            "20th anniversary edition of the timeless software development handbook."));
        _store.Add(new Guid("11111111-1111-1111-1111-1111111111c3"), P(
            new Guid("11111111-1111-1111-1111-1111111111c3"),
            "Designing Data-Intensive Applications", "designing-data-intensive-apps", 55.00m, FakeCategoryRepository.BooksId, 40,
            "Martin Kleppmann's deep dive into reliable, scalable, and maintainable data systems."));
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var p);
        return Task.FromResult(p);
    }

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var p = _store.Values.FirstOrDefault(x =>
            string.Equals(x.Slug, normalized, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(p);
    }

    public Task<(List<Product> Items, int TotalCount)> ListAsync(
        int page, int pageSize, Guid? categoryId = null, string? search = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Product> query = _store.Values;

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null && p.Description.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        var total = query.Count();

        var items = query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((items, total));
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (_store.ContainsKey(product.Id))
            throw new InvalidOperationException($"Product with ID {product.Id} already exists.");
        _store[product.Id] = product;
        return Task.CompletedTask;
    }

    public void Update(Product product)
    {
        if (!_store.ContainsKey(product.Id))
            throw new InvalidOperationException($"Product with ID {product.Id} not found.");
        _store[product.Id] = product;
    }

    public void Delete(Product product)
    {
        _store.Remove(product.Id);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var exists = _store.Values.Any(p =>
            string.Equals(p.Slug, normalized, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || p.Id != excludeId.Value));
        return Task.FromResult(exists);
    }

    public Task<Dictionary<Guid, Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, Product>();
        foreach (var id in ids.Distinct())
        {
            if (_store.TryGetValue(id, out var p)) result[id] = p;
        }
        return Task.FromResult(result);
    }
}
