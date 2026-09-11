using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Exceptions;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Products;

public record GetProductBySlugQuery(string Slug) : IRequest<ProductDetailResponse>;

public sealed class GetProductBySlugQueryValidator
    : AbstractValidator<GetProductBySlugQuery>
{
    public GetProductBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(220);
    }
}

public sealed class GetProductBySlugHandler
    : IRequestHandler<GetProductBySlugQuery, ProductDetailResponse>
{
    private readonly IProductRepository _products;
    private readonly IMapper _mapper;

    public GetProductBySlugHandler(IProductRepository products, IMapper mapper)
    {
        _products = products;
        _mapper = mapper;
    }

    public async Task<ProductDetailResponse> Handle(GetProductBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        var product = await _products.GetBySlugAsync(slug, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException<Product>(slug);
        }

        return _mapper.From(product).AdaptToType<ProductDetailResponse>();
    }
}
