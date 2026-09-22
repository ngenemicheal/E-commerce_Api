namespace ECommerce.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private readonly List<Product> _products = new();
    public IReadOnlyList<Product> Products => _products.AsReadOnly();

    public Category(Guid id, string name, string slug, string? description = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Category ID cannot be empty.", nameof(id));
        }

        ValidateName(name);
        ValidateSlug(slug);
        ValidateDescription(description);

        Id = id;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

#pragma warning disable CS8618
    private Category() { }
#pragma warning restore CS8618

    public void UpdateDetails(string name, string slug, string? description, DateTimeOffset now)
    {
        ValidateName(name);
        ValidateSlug(slug);
        ValidateDescription(description);

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = now;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be null or whitespace.", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(name));
        }
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Category slug cannot be null or whitespace.", nameof(slug));
        }

        if (slug.Length > 120)
        {
            throw new ArgumentException("Category slug cannot exceed 120 characters.", nameof(slug));
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (!string.IsNullOrWhiteSpace(description) && description.Length > 500)
        {
            throw new ArgumentException("Category description cannot exceed 500 characters.",
                nameof(description));
        }
    }
}
