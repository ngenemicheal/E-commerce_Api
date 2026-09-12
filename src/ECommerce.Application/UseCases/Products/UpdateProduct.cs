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

// public record UpdateProductCommand(
//     Guid Id,
//     string Name,
//     string Slug,
//     string? Description,
//     decimal PriceAmount,
//     string PriceCurrency = "USD",
//     Guid CategoryId = default,
//     int StockQuantity = 0,
//     string? ImageUrl = null) : IRequest<ProductResponse>;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    decimal PriceAmount,
    Guid CategoryId,
    string PriceCurrency = "USD",
    int StockQuantity = 0,
    string? ImageUrl = null) : IRequest<ProductResponse>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty().MaximumLength(220)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug may contain only lowercase letters, numbers, and single hyphens.");

        RuleFor(x => x.PriceAmount).GreaterThan(0m);
        RuleFor(x => x.PriceCurrency).NotEmpty().Length(3);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ProductResponse>
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _time;
    private readonly IMapper _mapper;

    public UpdateProductHandler(IProductRepository products, ICategoryRepository categories, IUnitOfWork unitOfWork, IDateTimeProvider time, IMapper mapper)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _time = time;
        _mapper = mapper;
    }

    public async Task<ProductResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException<Product>(request.Id);
        }

        var category = await _categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
        {
            throw new NotFoundException<Category>(request.CategoryId);
        }

        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await _products.SlugExistsAsync(slug, request.Id, cancellationToken))
        {
            throw new ConflictException($"A product with slug '{slug}' already exists.");
        }

        var price = Money.Create(request.PriceAmount, request.PriceCurrency);

        product.UpdateDetails(request.Name, slug, request.Description, request.ImageUrl, _time.UtcNowOffset);
        product.UpdatePrice(price, _time.UtcNowOffset);
        product.ChangeCategory(request.CategoryId, _time.UtcNowOffset);

        var deltaStock = request.StockQuantity - product.StockQuantity;
        if (deltaStock > 0)
        {
            product.IncreaseStock(deltaStock);
        }
        else if (deltaStock < 0)
        {
            product.DecreaseStock(-deltaStock);
        }

        _products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = _mapper.From(product).AdaptToType<ProductResponse>();
        return result with { CategoryName = category.Name };
    }
}
