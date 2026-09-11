using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.ValueObjects;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Products;

public record CreateProductCommand(
    string Name,
    string Slug,
    string? Description,
    decimal PriceAmount,
    string PriceCurrency = "USD",
    Guid CategoryId = default,
    int StockQuantity = 0,
    string? ImageUrl = null) : IRequest<ProductResponse>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Product slug is required.")
            .MaximumLength(220)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug may contain only lowercase letters, numbers, and single hyphens.");

        RuleFor(x => x.PriceAmount)
            .GreaterThan(0m)
            .WithMessage("Price amount must be greater than zero.");

        RuleFor(x => x.PriceCurrency)
            .NotEmpty()
            .WithMessage("Price currency is required.")
            .Length(3)
            .WithMessage("Currency must be a 3-character ISO code.");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("A category is required.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stock quantity cannot be negative.");
    }
}

public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateProductHandler(IProductRepository products, ICategoryRepository categories, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();

        if (await _products.SlugExistsAsync(slug, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A product with slug '{slug}' already exists.");
        }

        var category = await _categories.GetByIdAsync(request.CategoryId, cancellationToken) ?? throw new NotFoundException<Category>(request.CategoryId);

        var price = Money.Create(request.PriceAmount, request.PriceCurrency);

        var product = new Product(
            Guid.NewGuid(),
            request.Name,
            slug,
            price,
            request.CategoryId,
            request.StockQuantity,
            request.Description,
            request.ImageUrl);

        await _products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = _mapper.From(product).AdaptToType<ProductResponse>();
        return result with { CategoryName = category.Name };
    }
}
