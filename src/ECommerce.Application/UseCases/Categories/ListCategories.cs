using ECommerce.Application.DTOs.Categories;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Categories;

public record ListCategoriesQuery : IRequest<List<CategoryResponse>>;

public sealed class ListCategoriesQueryValidator : AbstractValidator<ListCategoriesQuery>
{
    public ListCategoriesQueryValidator()
    {
    }
}

public sealed class ListCategoriesHandler : IRequestHandler<ListCategoriesQuery, List<CategoryResponse>>
{
    private readonly ICategoryRepository _categories;
    private readonly IMapper _mapper;

    public ListCategoriesHandler(ICategoryRepository categories, IMapper mapper)
    {
        _categories = categories;
        _mapper = mapper;
    }

    public async Task<List<CategoryResponse>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        var list = await _categories.ListAsync(cancellationToken);
        return _mapper.From(list).AdaptToType<List<CategoryResponse>>();
    }
}
