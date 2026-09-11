namespace ECommerce.Application.DTOs.Identity;

public record RegisterRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName);

public record LoginRequest(
    string Email,
    string Password);

public record AuthResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    string TokenType = "Bearer");

public record UserProfileResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt);
