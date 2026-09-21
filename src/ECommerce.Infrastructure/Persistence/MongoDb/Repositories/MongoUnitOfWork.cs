using ECommerce.Domain.Interfaces;

namespace ECommerce.Infrastructure.Persistence.MongoDb.Repositories;

public class MongoUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}
