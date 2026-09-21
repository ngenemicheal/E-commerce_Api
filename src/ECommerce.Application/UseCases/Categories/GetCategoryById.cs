using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Exceptions;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Categories;

public record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDetailResponse>;

public sealed class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDetailResponse>
{
    private readonly ICategoryRepository _categories;
    private readonly IMapper _mapper;

    public GetCategoryByIdHandler(ICategoryRepository categories, IMapper mapper)
    {
        _categories = categories;
        _mapper = mapper;
    }

    public async Task<CategoryDetailResponse> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(request.Id, cancellationToken);
        return category is null
            ? throw new NotFoundException<Domain.Entities.Category>(request.Id)
            : _mapper.From(category).AdaptToType<CategoryDetailResponse>();
    }
}
