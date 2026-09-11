using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Exceptions;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Products;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDetailResponse>;

public sealed class GetProductByIdQueryValidator
    : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetProductByIdHandler
    : IRequestHandler<GetProductByIdQuery, ProductDetailResponse>
{
    private readonly IProductRepository _products;
    private readonly IMapper _mapper;

    public GetProductByIdHandler(IProductRepository products, IMapper mapper)
    {
        _products = products;
        _mapper = mapper;
    }

    public async Task<ProductDetailResponse> Handle(GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException<Product>(request.Id);
        }

        return _mapper.From(product).AdaptToType<ProductDetailResponse>();
    }
}
