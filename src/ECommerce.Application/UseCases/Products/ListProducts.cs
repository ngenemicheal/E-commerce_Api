using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Products;

public record ListProductsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    string? Search = null) : IRequest<PagedResponse<ProductResponse>>;

public sealed class ListProductsQueryValidator
    : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}

public sealed class ListProductsHandler
    : IRequestHandler<ListProductsQuery, PagedResponse<ProductResponse>>
{
    private readonly IProductRepository _products;
    private readonly IMapper _mapper;

    public ListProductsHandler(IProductRepository products, IMapper mapper)
    {
        _products = products;
        _mapper = mapper;
    }

    public async Task<PagedResponse<ProductResponse>> Handle(ListProductsQuery request,
        CancellationToken cancellationToken)
    {
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        var (items, totalCount) = await _products.ListAsync(
            page: request.Page,
            pageSize: request.PageSize,
            categoryId: request.CategoryId,
            search: search,
            cancellationToken: cancellationToken);

        var mapped = _mapper.From(items).AdaptToType<List<ProductResponse>>();

        for (var i = 0; i < mapped.Count; i++)
        {
            if (mapped[i].CategoryName is null)
            {
                var product = items[i];
                if (product.Category is not null)
                {
                    mapped[i] = mapped[i] with { CategoryName = product.Category.Name };
                }
            }
        }

        return new PagedResponse<ProductResponse>(
            mapped,
            totalCount,
            request.Page,
            request.PageSize);
    }
}
