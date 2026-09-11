using ECommerce.Application.Exceptions;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.UseCases.Categories;

public record DeleteCategoryCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteCategoryCommandValidator
    : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Category ID is required.");
    }
}

public sealed class DeleteCategoryHandler
    : IRequestHandler<DeleteCategoryCommand, Unit>
{
    private readonly ICategoryRepository _categories;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryHandler(
        ICategoryRepository categories,
        IProductRepository products,
        IUnitOfWork unitOfWork)
    {
        _categories = categories;
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            throw new NotFoundException<Domain.Entities.Category>(request.Id);
        }

        var (products, count) = await _products.ListAsync(
            page: 1, pageSize: 1, categoryId: request.Id,
            cancellationToken: cancellationToken);

        if (count > 0)
        {
            throw new ConflictException(
                $"Cannot delete category '{category.Name}' because it has {count} associated products.");
        }

        _categories.Delete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
