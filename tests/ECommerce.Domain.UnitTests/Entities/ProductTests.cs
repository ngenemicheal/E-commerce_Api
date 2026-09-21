using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.UnitTests.Entities;

public sealed class ProductTests
{
    private static Guid ValidId => Guid.NewGuid();
    private static Guid ValidCategoryId => Guid.NewGuid();
    private static Money ValidPrice => Money.Create(29.99m, "USD");

    [Fact]
    public void Constructor_ValidInputs_CreatesProduct()
    {
        var id = ValidId;
        var categoryId = ValidCategoryId;
        var price = ValidPrice;
        var product = new Product(id, "Widget", "widget", price, categoryId, 10, "A widget", "img.png");

        product.Id.Should().Be(id);
        product.Name.Should().Be("Widget");
        product.Slug.Should().Be("widget");
        product.Price.Should().Be(price);
        product.CategoryId.Should().Be(categoryId);
        product.StockQuantity.Should().Be(10);
        product.Description.Should().Be("A widget");
        product.ImageUrl.Should().Be("img.png");
    }

    [Fact]
    public void Constructor_EmptyId_Throws()
    {
        var action = () => new Product(Guid.Empty, "Name", "slug", ValidPrice, ValidCategoryId);
        action.Should().Throw<ArgumentException>().WithMessage("*ID cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_Throws(string? name)
    {
        var action = () => new Product(ValidId, name!, "slug", ValidPrice, ValidCategoryId);
        action.Should().Throw<ArgumentException>().WithMessage("*name cannot be null or whitespace*");
    }

    [Fact]
    public void Constructor_NameTooLong_Throws()
    {
        var action = () => new Product(ValidId, new string('A', 201), "slug", ValidPrice, ValidCategoryId);
        action.Should().Throw<ArgumentException>().WithMessage("*exceed 200 characters*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidSlug_Throws(string? slug)
    {
        var action = () => new Product(ValidId, "Name", slug!, ValidPrice, ValidCategoryId);
        action.Should().Throw<ArgumentException>().WithMessage("*slug cannot be null or whitespace*");
    }

    [Fact]
    public void Constructor_NullPrice_Throws()
    {
        var action = () => new Product(ValidId, "Name", "slug", null!, ValidCategoryId);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ZeroPrice_ThrowsInvalidPriceException()
    {
        var action = () => new Product(ValidId, "Name", "slug", Money.Zero(), ValidCategoryId);
        action.Should().Throw<InvalidPriceException>();
    }

    [Fact]
    public void Constructor_EmptyCategoryId_Throws()
    {
        var action = () => new Product(ValidId, "Name", "slug", ValidPrice, Guid.Empty);
        action.Should().Throw<ArgumentException>().WithMessage("*Category ID cannot be empty*");
    }

    [Fact]
    public void Constructor_NegativeStock_ThrowsInvalidStockQuantityException()
    {
        var action = () => new Product(ValidId, "Name", "slug", ValidPrice, ValidCategoryId, -1);
        action.Should().Throw<InvalidStockQuantityException>();
    }

    [Fact]
    public void Constructor_TrimsAndNormalizes()
    {
        var p = new Product(ValidId, "  Thing  ", "  THING-X  ", ValidPrice, ValidCategoryId, 0, "  Desc  ", "  http://img  ");
        p.Name.Should().Be("Thing");
        p.Slug.Should().Be("thing-x");
        p.Description.Should().Be("Desc");
        p.ImageUrl.Should().Be("http://img");
    }

    [Fact]
    public void Constructor_WhitespaceOptionals_Null()
    {
        var p = new Product(ValidId, "Name", "slug", ValidPrice, ValidCategoryId, 0, "   ", "   ");
        p.Description.Should().BeNull();
        p.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_Updates()
    {
        var p = new Product(ValidId, "N", "s", ValidPrice, ValidCategoryId);
        var now = DateTimeOffset.UtcNow.AddHours(1);
        p.UpdateDetails("New", "new-slug", "New desc", "new.png", now);
        p.Name.Should().Be("New");
        p.Slug.Should().Be("new-slug");
        p.Description.Should().Be("New desc");
        p.ImageUrl.Should().Be("new.png");
        p.UpdatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateDetails_InvalidName_Throws()
    {
        var p = new Product(ValidId, "N", "s", ValidPrice, ValidCategoryId);
        var action = () => p.UpdateDetails("", "s", null, null, DateTimeOffset.UtcNow);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdatePrice_Updates()
    {
        var p = new Product(ValidId, "N", "s", ValidPrice, ValidCategoryId);
        var newPrice = Money.Create(99.99m, "EUR");
        var now = DateTimeOffset.UtcNow;
        p.UpdatePrice(newPrice, now);
        p.Price.Should().Be(newPrice);
        p.UpdatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdatePrice_Null_Throws()
    {
        var p = new Product(ValidId, "N", "s", ValidPrice, ValidCategoryId);
        var action = () => p.UpdatePrice(null!, DateTimeOffset.UtcNow);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChangeCategory_Updates()
    {
        var p = new Product(ValidId, "N", "s", ValidPrice, ValidCategoryId);
        var newCat = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        p.ChangeCategory(newCat, now);
        p.CategoryId.Should().Be(newCat);
        p.UpdatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ChangeCategory_Empty_Throws()
    {
        var p = new Product(ValidId, "N", "s", ValidPrice, ValidCategoryId);
        var action = () => p.ChangeCategory(Guid.Empty, DateTimeOffset.UtcNow);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IncreaseStock_Adds()
    {
        var p = new Product(ValidId, "N", "s", ValidPrice, ValidCategoryId, 5);
        p.IncreaseStock(3);
        p.StockQuantity.Should().Be(8);
    }

    [Fact]
    public void IncreaseStock_Negative_Throws()
    {
        var p = new Product(ValidId, "N", "s", ValidPrice, ValidCategoryId);
        var action = () => p.IncreaseStock(-1);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DecreaseStock_Subtracts()
    {
        var p = new Product(ValidId, "N", "s", ValidPrice, ValidCategoryId, 10);
        p.DecreaseStock(4);
        p.StockQuantity.Should().Be(6);
    }

    [Fact]
    public void DecreaseStock_MoreThanAvailable_ThrowsInvalidStockQuantityException()
    {
        var p = new Product(ValidId, "N", "s", ValidPrice, ValidCategoryId, 2);
        var action = () => p.DecreaseStock(3);
        action.Should().Throw<InvalidStockQuantityException>();
    }

    [Fact]
    public void DecreaseStock_Negative_ThrowsArgumentOutOfRange()
    {
        var p = new Product(ValidId, "N", "s", ValidPrice, ValidCategoryId, 10);
        var action = () => p.DecreaseStock(-1);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
