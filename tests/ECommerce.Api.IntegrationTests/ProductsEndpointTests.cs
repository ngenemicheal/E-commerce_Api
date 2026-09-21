using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.UseCases.Identity;

namespace ECommerce.Api.IntegrationTests;

[Collection(nameof(ECommerceApiCollection))]
public sealed class ProductsEndpointTests
{
    private readonly ECommerceApiFactory _factory;

    public ProductsEndpointTests(ECommerceApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, string Jwt)> WithAdminAsync()
    {
        var client = _factory.CreateClient();
        var loginRsp = await client.PostAsJsonAsync("/api/identity/login",
            new LoginCommand(_factory.AdminEmail, _factory.AdminPassword));
        loginRsp.EnsureSuccessStatusCode();
        var auth = await loginRsp.Content.ReadFromJsonAsync<AuthStub>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);
        return (client, auth.Token);
    }

    private sealed record AuthStub(string Token);

    [Fact]
    public async Task ListProducts_NoAuth_Returns200()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.GetAsync("/api/products?page=1&pageSize=10");
        rsp.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await rsp.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        paged.Should().NotBeNull();
        paged!.Page.Should().Be(1);
    }

    [Fact]
    public async Task CreateProduct_AdminThenList_VisibleInList()
    {
        var t = await WithAdminAsync();
        using var client = t.Client;

        // Step 1: create category
        var categoryId = Guid.NewGuid();
        var slug = "cat-prod-" + Guid.NewGuid().ToString("N")[..8];
        var catContent = JsonContent.Create(new { Name = "Integration Category", Slug = slug, Description = "Desc" });
        var catRsp = await client.PostAsync("/api/categories", catContent);
        catRsp.EnsureSuccessStatusCode();
        var cat = await catRsp.Content.ReadFromJsonAsync<CategoryStub>();
        var actualCategoryId = cat!.Id;

        // Step 2: create product in that category
        var productSlug = "prod-" + Guid.NewGuid().ToString("N")[..8];
        var productContent = JsonContent.Create(new
        {
            Name = "Integration Widget",
            Slug = productSlug,
            Description = "A widget",
            PriceAmount = 49.99,
            PriceCurrency = "USD",
            CategoryId = actualCategoryId,
            StockQuantity = 25,
            ImageUrl = "https://example.com/widget.png"
        });

        var prodRsp = await client.PostAsync("/api/products", productContent);
        prodRsp.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await prodRsp.Content.ReadFromJsonAsync<ProductResponse>();
        product.Should().NotBeNull();
        product!.Slug.Should().Be(productSlug);
        product.Price.Amount.Should().Be(49.99m);
        product.StockQuantity.Should().Be(25);

        // Step 3: retrieve by id
        var getRsp = await client.GetAsync($"/api/products/{product.Id}");
        getRsp.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await getRsp.Content.ReadFromJsonAsync<ProductDetailResponse>();
        detail.Should().NotBeNull();
        detail!.Name.Should().Be("Integration Widget");

        // Step 4: retrieve by slug
        var slugRsp = await client.GetAsync($"/api/products/slug/{product.Slug}");
        slugRsp.StatusCode.Should().Be(HttpStatusCode.OK);
        var bySlug = await slugRsp.Content.ReadFromJsonAsync<ProductDetailResponse>();
        bySlug.Should().NotBeNull();
        bySlug!.Id.Should().Be(product.Id);
    }

    [Fact]
    public async Task GetProductById_NotFound_Returns404()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.GetAsync($"/api/products/{Guid.NewGuid()}");
        rsp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CategoryStub(Guid Id);
}
