using System.Net;
using System.Net.Http.Json;
using System.Text;
using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.UseCases.Identity;

namespace ECommerce.Api.IntegrationTests;

[Collection(nameof(ECommerceApiCollection))]
public sealed class CategoriesEndpointTests
{
    private readonly ECommerceApiFactory _factory;

    public CategoriesEndpointTests(ECommerceApiFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(object o)
    {
        return new StringContent(
            System.Text.Json.JsonSerializer.Serialize(o),
            Encoding.UTF8,
            "application/json");
    }

    private async Task<string> GetAdminJwtAsync(HttpClient client)
    {
        var loginRsp = await client.PostAsJsonAsync("/api/identity/login",
            new LoginCommand(_factory.AdminEmail, _factory.AdminPassword));
        loginRsp.EnsureSuccessStatusCode();
        var auth = await loginRsp.Content.ReadFromJsonAsync<AuthStub>();
        return auth!.Token;
    }

    private static void SetAuth(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task ListCategories_NoAuth_Returns200()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.GetAsync("/api/categories");
        rsp.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await rsp.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        items.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCategory_NoAuth_Returns401()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.PostAsync("/api/categories", Json(new CreateCategoryCommand("Gadgets", "gadgets", "Gadgets desc")));
        rsp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_AdminAuth_Returns201AndCanRetrieve()
    {
        using var client = _factory.CreateClient();
        var jwt = await GetAdminJwtAsync(client);
        SetAuth(client, jwt);

        var slug = "integration-gadgets-" + Guid.NewGuid().ToString("N")[..8];
        var createRsp = await client.PostAsync("/api/categories", Json(new CreateCategoryCommand("Integration Gadgets", slug, "Integration")));

        createRsp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRsp.Content.ReadFromJsonAsync<CategoryResponse>();
        created.Should().NotBeNull();
        created!.Slug.Should().Be(slug);
        created.Name.Should().Be("Integration Gadgets");
        createRsp.Headers.Location.Should().NotBeNull();

        var getRsp = await client.GetAsync($"/api/categories/{created.Id}");
        getRsp.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await getRsp.Content.ReadFromJsonAsync<CategoryDetailResponse>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task CreateCategory_DuplicateSlug_Returns409()
    {
        using var client = _factory.CreateClient();
        var jwt = await GetAdminJwtAsync(client);
        SetAuth(client, jwt);

        var slug = "dup-slug-" + Guid.NewGuid().ToString("N")[..8];
        var create1 = await client.PostAsync("/api/categories", Json(new CreateCategoryCommand("Name 1", slug, null)));
        create1.EnsureSuccessStatusCode();

        var create2 = await client.PostAsync("/api/categories", Json(new CreateCategoryCommand("Name 2", slug, null)));
        create2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private sealed record AuthStub(string Token, DateTimeOffset ExpiresAt, string TokenType = "Bearer");
    private sealed record CreateCategoryCommand(string Name, string Slug, string? Description);
}
