using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Categories;

public record CreateCategoryCommand(
    string Name,
    string Slug,
    string? Description) : IRequest<CategoryResponse>;

public sealed class CreateCategoryCommandValidator
    : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Category slug is required.")
            .MaximumLength(120).WithMessage("Category slug cannot exceed 120 characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug may contain only lowercase letters, numbers, and single hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Description))
            .WithMessage("Category description cannot exceed 500 characters.");
    }
}

public sealed class CreateCategoryHandler
    : IRequestHandler<CreateCategoryCommand, CategoryResponse>
{
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _time;
    private readonly IMapper _mapper;

    public CreateCategoryHandler(
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        IDateTimeProvider time,
        IMapper mapper)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _time = time;
        _mapper = mapper;
    }

    public async Task<CategoryResponse> Handle(CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();

        if (await _categories.SlugExistsAsync(slug, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A category with slug '{slug}' already exists.");
        }

        var category = new Category(Guid.NewGuid(), request.Name, slug, request.Description);
        await _categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.From(category).AdaptToType<CategoryResponse>();
    }
}
