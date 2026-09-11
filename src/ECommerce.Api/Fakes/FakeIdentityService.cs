using ECommerce.Application.DTOs.Identity;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;

namespace ECommerce.Api.Fakes;

public class FakeIdentityService : IIdentityService
{
    private sealed class FakeUser
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<string> Roles { get; } = new();
        public DateTimeOffset CreatedAt { get; set; }
    }

    private static readonly List<FakeUser> _users = new()
    {
        new FakeUser
        {
            Id = FakeCurrentUserService.CustomerId,
            Email = "customer@example.com",
            Password = "Customer123!",
            FirstName = "John",
            LastName = "Doe",
            Roles = { nameof(Role.Customer) },
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30)
        },
        new FakeUser
        {
            Id = FakeCurrentUserService.AdminId,
            Email = "admin@example.com",
            Password = "Admin123!",
            FirstName = "System",
            LastName = "Admin",
            Roles = { nameof(Role.Admin), nameof(Role.Customer) },
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-60)
        }
    };

    public Task<AuthResponse> RegisterCustomerAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = _users.FirstOrDefault(u =>
            string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            throw new ConflictException($"A user with email '{request.Email}' already exists.");

        var user = new FakeUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Password = request.Password,
            FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim(),
            Roles = { nameof(Role.Customer) },
            CreatedAt = DateTimeOffset.UtcNow
        };

        _users.Add(user);

        return Task.FromResult(new AuthResponse(
            Token: $"fake-jwt-{user.Id:N}",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
            TokenType: "Bearer"));
    }

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(u =>
            string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase) &&
            u.Password == request.Password);

        if (user is null)
            throw new UnauthorizedException("Invalid email or password.");

        return Task.FromResult(new AuthResponse(
            Token: $"fake-jwt-{user.Id:N}",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
            TokenType: "Bearer"));
    }

    public Task<UserProfileResponse> GetCurrentUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);

        if (user is null)
            throw new NotFoundException($"User with ID '{userId}' was not found.");

        return Task.FromResult(new UserProfileResponse(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Roles: user.Roles.AsReadOnly(),
            CreatedAt: user.CreatedAt));
    }

    public Task<bool> IsUserInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user is null) return Task.FromResult(false);
        return Task.FromResult(user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
}
