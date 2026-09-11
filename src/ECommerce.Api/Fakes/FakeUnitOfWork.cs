using ECommerce.Domain.Interfaces;

namespace ECommerce.Api.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }
}
