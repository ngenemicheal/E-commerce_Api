using ECommerce.Application.DTOs.Identity;
using ECommerce.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.UseCases.Identity;

public record LoginCommand(
    string Email,
    string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator
    : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public sealed class LoginHandler
    : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IIdentityService _identity;

    public LoginHandler(IIdentityService identity)
    {
        _identity = identity;
    }

    public Task<AuthResponse> Handle(LoginCommand request,
        CancellationToken cancellationToken)
    {
        var dto = new LoginRequest(request.Email, request.Password);
        return _identity.LoginAsync(dto, cancellationToken);
    }
}
