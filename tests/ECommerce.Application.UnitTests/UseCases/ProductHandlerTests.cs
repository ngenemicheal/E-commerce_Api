using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.UseCases.Products;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.ValueObjects;
using MapsterMapper;
using NSubstitute;

namespace ECommerce.Application.UnitTests.UseCases;

public sealed class ProductHandlerTests
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ProductHandlerTests()
    {
        _products = Substitute.For<IProductRepository>();
        _categories = Substitute.For<ICategoryRepository>();
        _uow = Substitute.For<IUnitOfWork>();
        _mapper = TestHelper.CreateMapper();
    }

    private static void SetCategoryNav(Product p, Category c)
        => p.GetType().GetProperty("Category")?.SetValue(p, c);

    [Fact]
    public async Task CreateProduct_ValidData_CreatesAndReturns()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category(categoryId, "Books", "books");
        _categories.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns(category);
        _products.SlugExistsAsync("great-book", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new CreateProductCommand(
            Name: "Great Book",
            Slug: "Great-Book",
            Description: "Must read",
            PriceAmount: 29.99m,
            CategoryId: categoryId,
            PriceCurrency: "USD",
            StockQuantity: 50,
            ImageUrl: "img.png");

        var handler = new CreateProductHandler(_products, _categories, _uow, _mapper);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Great Book");
        result.Slug.Should().Be("great-book");
        result.Price.Amount.Should().Be(29.99m);
        result.Price.Currency.Should().Be("USD");
        result.StockQuantity.Should().Be(50);
        result.CategoryName.Should().Be("Books");
        await _products.Received(1).AddAsync(Arg.Is<Product>(p => p.Name == "Great Book"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProduct_DuplicateSlug_ThrowsConflict()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category(categoryId, "Books", "books");
        _categories.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns(category);
        _products.SlugExistsAsync("great-book", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new CreateProductCommand("Great Book", "great-book", null, 10m, categoryId);
        var handler = new CreateProductHandler(_products, _categories, _uow, _mapper);
        var action = () => handler.Handle(cmd, CancellationToken.None);
        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateProduct_MissingCategory_ThrowsNotFound()
    {
        var categoryId = Guid.NewGuid();
        _categories.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns((Category?)null);
        _products.SlugExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new CreateProductCommand("Book", "book", null, 10m, categoryId);
        var handler = new CreateProductHandler(_products, _categories, _uow, _mapper);
        var action = () => handler.Handle(cmd, CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException<Category>>();
    }

    [Fact]
    public async Task ListProducts_ReturnsPagedResponse()
    {
        var categoryId = Guid.NewGuid();
        var cat = new Category(categoryId, "Books", "books");
        var products = new List<Product>
        {
            new(Guid.NewGuid(), "Book A", "book-a", Money.Create(10m, "USD"), categoryId, 5),
            new(Guid.NewGuid(), "Book B", "book-b", Money.Create(20m, "USD"), categoryId, 3),
        };
        SetCategoryNav(products[0], cat);
        SetCategoryNav(products[1], cat);

        _products.ListAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns((products, 2));

        var handler = new ListProductsHandler(_products, _mapper);
        var result = await handler.Handle(new ListProductsQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Count.Should().Be(2);
        result.Items[0].Slug.Should().Be("book-a");
        result.Items[0].CategoryName.Should().Be("Books");
    }

    [Fact]
    public async Task GetProductById_Found_ReturnsDetail()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var cat = new Category(categoryId, "Books", "books");
        var product = new Product(productId, "Book", "book", Money.Create(9.99m, "USD"), categoryId, 10);
        SetCategoryNav(product, cat);

        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new GetProductByIdHandler(_products, _mapper);
        var result = await handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Category.Should().NotBeNull();
        result.Category!.Name.Should().Be("Books");
    }

    [Fact]
    public async Task GetProductById_NotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _products.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);
        var handler = new GetProductByIdHandler(_products, _mapper);
        var action = () => handler.Handle(new GetProductByIdQuery(id), CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException<Product>>();
    }

    [Fact]
    public async Task GetProductBySlug_Found_ReturnsDetail()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var cat = new Category(categoryId, "Books", "books");
        var product = new Product(productId, "Book", "my-book", Money.Create(9.99m, "USD"), categoryId, 10);
        SetCategoryNav(product, cat);

        _products.GetBySlugAsync("my-book", Arg.Any<CancellationToken>()).Returns(product);
        var handler = new GetProductBySlugHandler(_products, _mapper);
        var result = await handler.Handle(new GetProductBySlugQuery("my-book"), CancellationToken.None);

        result.Should().NotBeNull();
        result.Slug.Should().Be("my-book");
    }

    [Fact]
    public async Task GetProductBySlug_NotFound_ThrowsNotFoundException()
    {
        _products.GetBySlugAsync("missing", Arg.Any<CancellationToken>()).Returns((Product?)null);
        var handler = new GetProductBySlugHandler(_products, _mapper);
        var action = () => handler.Handle(new GetProductBySlugQuery("missing"), CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException<Product>>();
    }
}
