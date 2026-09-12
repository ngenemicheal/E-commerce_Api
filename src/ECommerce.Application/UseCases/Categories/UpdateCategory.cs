using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace ECommerce.Application.UseCases.Categories;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string Slug,
    string? Description) : IRequest<CategoryResponse>;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Category slug is required.")
            .MaximumLength(120)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug may contain only lowercase letters, numbers, and single hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

public sealed class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, CategoryResponse>
{
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _time;
    private readonly IMapper _mapper;

    public UpdateCategoryHandler(
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

    public async Task<CategoryResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException<Domain.Entities.Category>(request.Id);
        var slug = request.Slug.Trim().ToLowerInvariant();

        if (await _categories.SlugExistsAsync(slug, request.Id, cancellationToken))
        {
            throw new ConflictException($"A category with slug '{slug}' already exists.");
        }

        category.UpdateDetails(request.Name, slug, request.Description, _time.UtcNowOffset);
        _categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.From(category).AdaptToType<CategoryResponse>();
    }
}
