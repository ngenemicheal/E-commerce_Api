using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.UseCases.Categories;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MapsterMapper;
using MediatR;
using NSubstitute;

namespace ECommerce.Application.UnitTests.UseCases;

public sealed class CategoryHandlerTests
{
    private readonly ICategoryRepository _categories;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _time;
    private readonly IMapper _mapper;

    public CategoryHandlerTests()
    {
        _categories = Substitute.For<ICategoryRepository>();
        _products = Substitute.For<IProductRepository>();
        _uow = Substitute.For<IUnitOfWork>();
        _time = TestHelper.CreateDateTimeProvider();
        _mapper = TestHelper.CreateMapper();
    }

    [Fact]
    public async Task CreateCategory_WhenUnique_CreatesAndReturnsResponse()
    {
        var cmd = new CreateCategoryCommand("Books", "books", "Read all the things");
        _categories.SlugExistsAsync("books", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateCategoryHandler(_categories, _uow, _time, _mapper);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Books");
        result.Slug.Should().Be("books");
        result.Description.Should().Be("Read all the things");
        await _categories.Received(42).AddAsync(Arg.Is<Category>(c => c.Name == "Books"), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCategory_NormalizesSlugToLower()
    {
        var cmd = new CreateCategoryCommand("Books", "  BOOKS-CLASSICS  ", null);
        _categories.SlugExistsAsync("books-classics", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateCategoryHandler(_categories, _uow, _time, _mapper);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Slug.Should().Be("books-classics");
    }

    [Fact]
    public async Task CreateCategory_DuplicateSlug_ThrowsConflictException()
    {
        var cmd = new CreateCategoryCommand("Books", "books", null);
        _categories.SlugExistsAsync("books", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateCategoryHandler(_categories, _uow, _time, _mapper);
        var action = () => handler.Handle(cmd, CancellationToken.None);
        await action.Should().ThrowAsync<ConflictException>().WithMessage("*slug*already exists*");
        await _categories.DidNotReceive().AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListCategories_ReturnsAll()
    {
        var cats = new List<Category>
        {
            new(Guid.NewGuid(), "Books", "books"),
            new(Guid.NewGuid(), "Electronics", "electronics"),
        };
        _categories.ListAsync(Arg.Any<CancellationToken>()).Returns(cats);

        var handler = new ListCategoriesHandler(_categories, _mapper);
        var result = await handler.Handle(new ListCategoriesQuery(), CancellationToken.None);

        result.Count.Should().Be(2);
        result.Select(c => c.Slug).Should().Contain(new[] { "books", "electronics" });
    }

    [Fact]
    public async Task GetCategoryById_WhenFound_ReturnsDetail()
    {
        var catId = Guid.NewGuid();
        var cat = new Category(catId, "Books", "books", "desc");
        _categories.GetByIdAsync(catId, Arg.Any<CancellationToken>()).Returns(cat);

        var handler = new GetCategoryByIdHandler(_categories, _mapper);
        var result = await handler.Handle(new GetCategoryByIdQuery(catId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Books");
    }

    [Fact]
    public async Task GetCategoryById_WhenMissing_ThrowsNotFoundException()
    {
        var catId = Guid.NewGuid();
        _categories.GetByIdAsync(catId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var handler = new GetCategoryByIdHandler(_categories, _mapper);
        var action = () => handler.Handle(new GetCategoryByIdQuery(catId), CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException<Category>>();
    }

    [Fact]
    public async Task UpdateCategory_WhenFound_Updates()
    {
        var catId = Guid.NewGuid();
        var cat = new Category(catId, "Books", "books", "old");
        _categories.GetByIdAsync(catId, Arg.Any<CancellationToken>()).Returns(cat);
        _categories.SlugExistsAsync("new-slug", catId, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new UpdateCategoryHandler(_categories, _uow, _time, _mapper);
        var result = await handler.Handle(new UpdateCategoryCommand(catId, "NewName", "new-slug", "New desc"), CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("NewName");
        result.Slug.Should().Be("new-slug");
        _categories.Received(1).Update(cat);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCategory_WhenMissing_ThrowsNotFoundException()
    {
        var catId = Guid.NewGuid();
        _categories.GetByIdAsync(catId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var handler = new UpdateCategoryHandler(_categories, _uow, _time, _mapper);
        var action = () => handler.Handle(new UpdateCategoryCommand(catId, "N", "s", null), CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException<Category>>();
    }

    [Fact]
    public async Task UpdateCategory_DuplicateSlug_ThrowsConflict()
    {
        var catId = Guid.NewGuid();
        var cat = new Category(catId, "Old", "old");
        _categories.GetByIdAsync(catId, Arg.Any<CancellationToken>()).Returns(cat);
        _categories.SlugExistsAsync("taken", catId, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new UpdateCategoryHandler(_categories, _uow, _time, _mapper);
        var action = () => handler.Handle(new UpdateCategoryCommand(catId, "Name", "taken", null), CancellationToken.None);
        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeleteCategory_WhenFound_Deletes()
    {
        var catId = Guid.NewGuid();
        var cat = new Category(catId, "Books", "books");
        _categories.GetByIdAsync(catId, Arg.Any<CancellationToken>()).Returns(cat);
        _products.ListAsync(1, 1, catId, null, Arg.Any<CancellationToken>()).Returns((new List<Product>(), 0));

        var handler = new DeleteCategoryHandler(_categories, _products, _uow);
        var result = await handler.Handle(new DeleteCategoryCommand(catId), CancellationToken.None);

        result.Should().Be(Unit.Value);
        _categories.Received(1).Delete(cat);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCategory_WhenMissing_ThrowsNotFoundException()
    {
        var catId = Guid.NewGuid();
        _categories.GetByIdAsync(catId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var handler = new DeleteCategoryHandler(_categories, _products, _uow);
        var action = () => handler.Handle(new DeleteCategoryCommand(catId), CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException<Category>>();
    }

    [Fact]
    public async Task DeleteCategory_WithAssociatedProducts_ThrowsConflict()
    {
        var catId = Guid.NewGuid();
        var cat = new Category(catId, "Books", "books");
        _categories.GetByIdAsync(catId, Arg.Any<CancellationToken>()).Returns(cat);
        _products.ListAsync(1, 1, catId, null, Arg.Any<CancellationToken>()).Returns((new List<Product>(), 5));

        var handler = new DeleteCategoryHandler(_categories, _products, _uow);
        var action = () => handler.Handle(new DeleteCategoryCommand(catId), CancellationToken.None);
        await action.Should().ThrowAsync<ConflictException>().WithMessage("*5 associated products*");
    }
}
