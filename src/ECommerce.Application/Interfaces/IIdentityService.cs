using ECommerce.Application.DTOs.Identity;

namespace ECommerce.Application.Interfaces;

public interface IIdentityService
{
    Task<AuthResponse> RegisterCustomerAsync(RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<UserProfileResponse> GetCurrentUserProfileAsync(Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsUserInRoleAsync(Guid userId, string role,
        CancellationToken cancellationToken = default);
}
