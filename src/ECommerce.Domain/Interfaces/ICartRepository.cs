using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByCustomerIdWithItemsAsync(Guid customerId,
        CancellationToken cancellationToken = default);

    Task<Cart?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Cart cart, CancellationToken cancellationToken = default);
    void Update(Cart cart);
    void Delete(Cart cart);
}
