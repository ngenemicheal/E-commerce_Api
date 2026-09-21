using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.DTOs.Identity;
using ECommerce.Application.UseCases.Identity;

namespace ECommerce.Api.IntegrationTests;

[Collection(nameof(ECommerceApiCollection))]
public sealed class IdentityEndpointTests
{
    private readonly ECommerceApiFactory _factory;

    public IdentityEndpointTests(ECommerceApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_AdminCredentials_Returns200WithToken()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.PostAsJsonAsync("/api/identity/login",
            new LoginCommand(_factory.AdminEmail, _factory.AdminPassword));

        rsp.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await rsp.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Token.Should().NotBeNullOrEmpty();
        auth.TokenType.Should().Be("Bearer");
        auth.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.PostAsJsonAsync("/api/identity/login",
            new LoginCommand(_factory.AdminEmail, "WrongPassword123!"));
        rsp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_NoAuth_Returns401()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.GetAsync("/api/identity/me");
        rsp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithAuth_Returns200WithProfile()
    {
        using var client = _factory.CreateClient();
        var loginRsp = await client.PostAsJsonAsync("/api/identity/login",
            new LoginCommand(_factory.AdminEmail, _factory.AdminPassword));
        loginRsp.EnsureSuccessStatusCode();
        var auth = await loginRsp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);

        var meRsp = await client.GetAsync("/api/identity/me");
        meRsp.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await meRsp.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(_factory.AdminEmail);
        profile.Roles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_NewCustomer_Returns200AndCanLogin()
    {
        using var client = _factory.CreateClient();
        var email = $"cust-{Guid.NewGuid():N}@test.local";
        const string password = "Customer@123456";
        var regRsp = await client.PostAsJsonAsync("/api/identity/register",
            new RegisterCommand(email, password, "Test", "User"));

        regRsp.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await regRsp.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Token.Should().NotBeNullOrEmpty();

        var loginRsp = await client.PostAsJsonAsync("/api/identity/login",
            new LoginCommand(email, password));
        loginRsp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        using var client = _factory.CreateClient();
        var email = $"dupe-{Guid.NewGuid():N}@test.local";
        const string password = "Customer@123456";
        var reg1 = await client.PostAsJsonAsync("/api/identity/register",
            new RegisterCommand(email, password, "First", "User"));
        reg1.EnsureSuccessStatusCode();

        var reg2 = await client.PostAsJsonAsync("/api/identity/register",
            new RegisterCommand(email, password, "Second", "User"));
        reg2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
