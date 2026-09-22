using ECommerce.Application.DTOs.Identity;
using ECommerce.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.UseCases.Identity;

public record RegisterCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName) : IRequest<AuthResponse>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(254);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.FirstName)
            .MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.LastName));
    }
}

public sealed class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IIdentityService _identity;

    public RegisterHandler(IIdentityService identity)
    {
        _identity = identity;
    }

    public Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var dto = new RegisterRequest(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName);

        return _identity.RegisterCustomerAsync(dto, cancellationToken);
    }
}
