using ECommerce.Application.DTOs.Identity;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AppUserT = ECommerce.Infrastructure.Identity.AppUser;

namespace ECommerce.Infrastructure.Identity;

public class IdentityService(
    UserManager<AppUserT> userManager,
    IOptions<JwtSettings> jwtSettings)
    : IIdentityService
{
    public async Task<AuthResponse> RegisterCustomerAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new ConflictException($"A user with email '{request.Email}' already exists.");
        }

        var user = new AppUserT
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
            throw new ConflictException($"Failed to create user: {errors}");
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, Role.Customer.ToString());
        if (!addRoleResult.Succeeded)
        {
            var errors = string.Join(" ", addRoleResult.Errors.Select(e => e.Description));
            throw new ConflictException($"Failed to assign role: {errors}");
        }

        return await GenerateAuthResponse(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var signInResult = await userManager.CheckPasswordAsync(user, request.Password);
        if (!signInResult)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return await GenerateAuthResponse(user, cancellationToken);
    }

    public async Task<UserProfileResponse> GetCurrentUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new NotFoundException($"User with id '{userId}' not found.");
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList().AsReadOnly();

        return new UserProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            roles,
            user.CreatedAt);
    }

    public async Task<bool> IsUserInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        return await userManager.IsInRoleAsync(user, role);
    }

    private async Task<AuthResponse> GenerateAuthResponse(AppUserT user, CancellationToken _)
    {
        var settings = jwtSettings.Value;
        var userRoles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(settings.ExpiresInMinutes);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.WriteToken(token);

        return new AuthResponse(jwt, new DateTimeOffset(expires));
    }
}
