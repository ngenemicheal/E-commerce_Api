using ECommerce.Application.Interfaces;

namespace ECommerce.Api.Fakes;

public class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
}
