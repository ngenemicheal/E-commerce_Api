using System.Security.Claims;
using System.Text.Encodings.Web;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ECommerce.Api.Fakes;

public class FakeAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ICurrentUserService _currentUser;

    public FakeAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ICurrentUserService currentUser)
        : base(options, logger, encoder)
    {
        _currentUser = currentUser;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _currentUser.UserId.Value.ToString()),
            new(ClaimTypes.Name, _currentUser.Email ?? string.Empty),
            new(ClaimTypes.Email, _currentUser.Email ?? string.Empty)
        };

        foreach (var role in _currentUser.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
