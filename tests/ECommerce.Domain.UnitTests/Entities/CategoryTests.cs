using ECommerce.Domain.Entities;

namespace ECommerce.Domain.UnitTests.Entities;

public sealed class CategoryTests
{
    private static Guid ValidId => Guid.NewGuid();

    [Fact]
    public void Constructor_ValidInputs_CreatesCategory()
    {
        var id = ValidId;
        var cat = new Category(id, "Electronics", "electronics", "Gadgets");
        cat.Id.Should().Be(id);
        cat.Name.Should().Be("Electronics");
        cat.Slug.Should().Be("electronics");
        cat.Description.Should().Be("Gadgets");
        cat.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var action = () => new Category(Guid.Empty, "Name", "slug");
        action.Should().Throw<ArgumentException>().WithMessage("*ID cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string? name)
    {
        var action = () => new Category(ValidId, name!, "slug");
        action.Should().Throw<ArgumentException>().WithMessage("*name cannot be null or whitespace*");
    }

    [Fact]
    public void Constructor_NameTooLong_ThrowsArgumentException()
    {
        var longName = new string('A', 101);
        var action = () => new Category(ValidId, longName, "slug");
        action.Should().Throw<ArgumentException>().WithMessage("*exceed 100 characters*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidSlug_ThrowsArgumentException(string? slug)
    {
        var action = () => new Category(ValidId, "Name", slug!);
        action.Should().Throw<ArgumentException>().WithMessage("*slug cannot be null or whitespace*");
    }

    [Fact]
    public void Constructor_SlugTooLong_ThrowsArgumentException()
    {
        var longSlug = new string('a', 121);
        var action = () => new Category(ValidId, "Name", longSlug);
        action.Should().Throw<ArgumentException>().WithMessage("*exceed 120 characters*");
    }

    [Fact]
    public void Constructor_DescriptionTooLong_ThrowsArgumentException()
    {
        var longDesc = new string('x', 501);
        var action = () => new Category(ValidId, "Name", "slug", longDesc);
        action.Should().Throw<ArgumentException>().WithMessage("*exceed 500 characters*");
    }

    [Fact]
    public void Constructor_TrimsAndNormalizes()
    {
        var cat = new Category(ValidId, "  Books  ", "  Books-CLASSICS  ", "  Good reads  ");
        cat.Name.Should().Be("Books");
        cat.Slug.Should().Be("books-classics");
        cat.Description.Should().Be("Good reads");
    }

    [Fact]
    public void Constructor_WhitespaceDescription_SetsNull()
    {
        var cat = new Category(ValidId, "Name", "slug", "   ");
        cat.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_UpdatesValues()
    {
        var cat = new Category(ValidId, "OldName", "old-slug", "Old desc");
        var now = DateTimeOffset.UtcNow.AddHours(1);
        cat.UpdateDetails("NewName", "new-slug", "New desc", now);
        cat.Name.Should().Be("NewName");
        cat.Slug.Should().Be("new-slug");
        cat.Description.Should().Be("New desc");
        cat.UpdatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateDetails_InvalidName_Throws()
    {
        var cat = new Category(ValidId, "Name", "slug");
        var action = () => cat.UpdateDetails("", "slug", null, DateTimeOffset.UtcNow);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Products_InitiallyEmpty()
    {
        var cat = new Category(ValidId, "Name", "slug");
        cat.Products.Should().BeEmpty();
    }
}
