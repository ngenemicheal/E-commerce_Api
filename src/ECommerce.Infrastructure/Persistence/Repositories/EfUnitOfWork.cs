using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class EfUnitOfWork(ECommerceDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
