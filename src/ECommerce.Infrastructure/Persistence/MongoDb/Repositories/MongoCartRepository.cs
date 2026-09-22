using System.Reflection;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MongoDB.Driver;

namespace ECommerce.Infrastructure.Persistence.MongoDb.Repositories;

public class MongoCartRepository(IMongoDatabase db) : ICartRepository
{
    private readonly IMongoCollection<Cart> _col = db.GetCollection<Cart>("carts");
    private readonly IMongoCollection<Product> _products = db.GetCollection<Product>("products");

    private static readonly FieldInfo ItemsField =
        typeof(Cart).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public async Task<Cart?> GetByCustomerIdWithItemsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var cart = await _col.Find(c => c.CustomerId == customerId).FirstOrDefaultAsync(cancellationToken);
        if (cart is not null)
        {
            await HydrateProductsAsync(new[] { cart }, cancellationToken);
        }
        return cart;
    }

    public async Task<Cart?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cart = await _col.Find(c => c.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (cart is not null)
        {
            await HydrateProductsAsync(new[] { cart }, cancellationToken);
        }
        return cart;
    }

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _col.InsertOneAsync(cart, null, cancellationToken);
    }

    public void Update(Cart cart)
    {
        _col.ReplaceOne(c => c.Id == cart.Id, cart, new ReplaceOptions { IsUpsert = false });
    }

    public void Delete(Cart cart)
    {
        _col.DeleteOne(c => c.Id == cart.Id);
    }

    private async Task HydrateProductsAsync(IEnumerable<Cart> carts, CancellationToken ct)
    {
        var cartList = carts.ToList();
        if (cartList.Count == 0)
        {
            return;
        }

        var allProductIds = new List<Guid>();
        foreach (var c in cartList)
        {
            var items = (List<CartItem>?)ItemsField.GetValue(c);
            if (items is not null)
            {
                allProductIds.AddRange(items.Select(i => i.ProductId));
            }
        }

        allProductIds = allProductIds.Distinct().Where(id => id != Guid.Empty).ToList();
        if (allProductIds.Count == 0)
        {
            return;
        }

        var productCol = db.GetCollection<Product>("products");
        var products = await productCol.Find(Builders<Product>.Filter.In(p => p.Id, allProductIds))
            .ToListAsync(ct);

        var categoryIds = products.Select(p => p.CategoryId).Where(id => id != Guid.Empty).Distinct().ToList();
        Dictionary<Guid, Category>? catMap = null;
        if (categoryIds.Count > 0)
        {
            var catCol = db.GetCollection<Category>("categories");
            var cats = await catCol.Find(Builders<Category>.Filter.In(c => c.Id, categoryIds)).ToListAsync(ct);
            catMap = cats.ToDictionary(c => c.Id);
        }

        var productMap = products.ToDictionary(p => p.Id);
        var productCategoryProp = typeof(Product).GetProperty("Category", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var cartItemProductProp = typeof(CartItem).GetProperty("Product", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (catMap is not null && productCategoryProp is not null)
        {
            foreach (var p in products)
            {
                if (catMap.TryGetValue(p.CategoryId, out var c))
                {
                    productCategoryProp.SetValue(p, c);
                }
            }
        }

        foreach (var cart in cartList)
        {
            var items = (List<CartItem>?)ItemsField.GetValue(cart);
            if (items is null || cartItemProductProp is null)
            {
                continue;
            }

            foreach (var item in items)
            {
                if (productMap.TryGetValue(item.ProductId, out var prod))
                {
                    cartItemProductProp.SetValue(item, prod);
                }
            }
        }
    }
}
