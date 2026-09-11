using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Api.Fakes;

public class FakeCartRepository : ICartRepository
{
    private static readonly Dictionary<Guid, Cart> _byId = new();
    private static readonly Dictionary<Guid, Guid> _customerToCartId = new();

    public Task<Cart?> GetByCustomerIdWithItemsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        if (_customerToCartId.TryGetValue(customerId, out var cartId) && _byId.TryGetValue(cartId, out var cart))
            return Task.FromResult<Cart?>(cart);
        return Task.FromResult<Cart?>(null);
    }

    public Task<Cart?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var cart);
        return Task.FromResult<Cart?>(cart);
    }

    public Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        if (_byId.ContainsKey(cart.Id))
            throw new InvalidOperationException($"Cart with ID {cart.Id} already exists.");
        _byId[cart.Id] = cart;
        _customerToCartId[cart.CustomerId] = cart.Id;
        return Task.CompletedTask;
    }

    public void Update(Cart cart)
    {
        if (!_byId.ContainsKey(cart.Id))
            throw new InvalidOperationException($"Cart with ID {cart.Id} not found.");
        _byId[cart.Id] = cart;
    }

    public void Delete(Cart cart)
    {
        _byId.Remove(cart.Id);
        if (_customerToCartId.TryGetValue(cart.CustomerId, out var id) && id == cart.Id)
            _customerToCartId.Remove(cart.CustomerId);
    }
}
