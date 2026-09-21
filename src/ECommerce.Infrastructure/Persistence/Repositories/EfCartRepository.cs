using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class EfCartRepository(ECommerceDbContext context) : ICartRepository
{
    public async Task<Cart?> GetByCustomerIdWithItemsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task<Cart?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await context.Carts.AddAsync(cart, cancellationToken);
    }

    public void Update(Cart cart)
    {
        context.Carts.Update(cart);
    }

    public void Delete(Cart cart)
    {
        context.Carts.Remove(cart);
    }
}
