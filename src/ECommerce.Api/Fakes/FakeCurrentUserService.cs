using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;

namespace ECommerce.Api.Fakes;

public class FakeCurrentUserService : ICurrentUserService
{
    public static readonly Guid CustomerId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AdminId = new("22222222-2222-2222-2222-222222222222");

    private readonly bool _isAuthenticated;
    private readonly bool _isAdmin;

    public FakeCurrentUserService(bool isAuthenticated = true, bool isAdmin = false)
    {
        _isAuthenticated = isAuthenticated;
        _isAdmin = isAdmin;
    }

    public Guid? UserId => _isAuthenticated ? (_isAdmin ? AdminId : CustomerId) : null;
    public string? Email => _isAuthenticated ? (_isAdmin ? "admin@example.com" : "customer@example.com") : null;

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (!_isAuthenticated) return Array.Empty<string>();
            return _isAdmin
                ? new[] { nameof(Role.Admin), nameof(Role.Customer) }
                : new[] { nameof(Role.Customer) };
        }
    }

    public bool IsAuthenticated => _isAuthenticated;

    public bool IsInRole(string role)
    {
        if (!_isAuthenticated) return false;
        if (string.Equals(role, nameof(Role.Customer), StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(role, nameof(Role.Admin), StringComparison.OrdinalIgnoreCase)) return _isAdmin;
        return false;
    }

    public Guid GetUserIdOrThrow()
    {
        if (!_isAuthenticated || UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");
        return UserId.Value;
    }
}
