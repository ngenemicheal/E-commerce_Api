using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Api.Fakes;

public class FakeCategoryRepository : ICategoryRepository
{
    private static readonly Dictionary<Guid, Category> _store;

    public static readonly Guid ElectronicsId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid ClothingId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid BooksId = new("cccccccc-cccc-cccc-cccc-cccccccccccc");

    static FakeCategoryRepository()
    {
        _store = new Dictionary<Guid, Category>
        {
            [ElectronicsId] = new Category(
                id: ElectronicsId,
                name: "Electronics",
                slug: "electronics",
                description: "Cutting-edge gadgets, phones, laptops, and home electronics."),
            [ClothingId] = new Category(
                id: ClothingId,
                name: "Clothing",
                slug: "clothing",
                description: "Apparel for men, women, and children: casual, formal, and sportswear."),
            [BooksId] = new Category(
                id: BooksId,
                name: "Books",
                slug: "books",
                description: "Fiction, non-fiction, technical, and children's literature.")
        };
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var c);
        return Task.FromResult(c);
    }

    public Task<List<Category>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.Values.ToList());
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        if (_store.ContainsKey(category.Id))
            throw new InvalidOperationException($"Category with ID {category.Id} already exists.");
        _store[category.Id] = category;
        return Task.CompletedTask;
    }

    public void Update(Category category)
    {
        if (!_store.ContainsKey(category.Id))
            throw new InvalidOperationException($"Category with ID {category.Id} not found.");
        _store[category.Id] = category;
    }

    public void Delete(Category category)
    {
        _store.Remove(category.Id);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var exists = _store.Values.Any(c =>
            string.Equals(c.Slug, normalized, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || c.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}
