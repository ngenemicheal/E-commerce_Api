using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public Money Price { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public int StockQuantity { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public Product(Guid id, string name, string slug, Money price, Guid categoryId, int stockQuantity = 0, string? description = null, string? imageUrl = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty.", nameof(id));
        }

        ValidateName(name);
        ValidateSlug(slug);

        if (price is null)
        {
            throw new ArgumentNullException(nameof(price));
        }

        if (price.Amount <= 0m)
        {
            throw new InvalidPriceException(price.Amount, price.Currency);
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category ID cannot be empty.", nameof(categoryId));
        }

        if (stockQuantity < 0)
        {
            throw new InvalidStockQuantityException(0, stockQuantity);
        }

        Id = id;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Price = price;
        CategoryId = categoryId;
        StockQuantity = stockQuantity;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

#pragma warning disable CS8618
    private Product() { }
#pragma warning restore CS8618

    public void UpdateDetails(string name, string slug, string? description, string? imageUrl, DateTimeOffset now)
    {
        ValidateName(name);
        ValidateSlug(slug);

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        UpdatedAt = now;
    }

    public void UpdatePrice(Money newPrice, DateTimeOffset now)
    {
        if (newPrice is null)
        {
            throw new ArgumentNullException(nameof(newPrice));
        }

        if (newPrice.Amount <= 0m)
        {
            throw new InvalidPriceException(newPrice.Amount, newPrice.Currency);
        }

        Price = newPrice;
        UpdatedAt = now;
    }

    public void ChangeCategory(Guid categoryId, DateTimeOffset now)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category ID cannot be empty.", nameof(categoryId));
        }

        CategoryId = categoryId;
        UpdatedAt = now;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity),
                "Quantity to add cannot be negative.");
        }

        StockQuantity += quantity;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity),
                "Quantity to remove cannot be negative.");
        }

        if (StockQuantity < quantity)
        {
            throw new InvalidStockQuantityException(quantity, StockQuantity);
        }

        StockQuantity -= quantity;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name cannot be null or whitespace.", nameof(name));
        }

        if (name.Length > 200)
        {
            throw new ArgumentException("Product name cannot exceed 200 characters.", nameof(name));
        }
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Product slug cannot be null or whitespace.", nameof(slug));
        }

        if (slug.Length > 220)
        {
            throw new ArgumentException("Product slug cannot exceed 220 characters.", nameof(slug));
        }
    }
}
