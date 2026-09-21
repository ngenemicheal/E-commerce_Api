using ECommerce.Application.DTOs.Identity;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.UseCases.Identity;

public record GetCurrentUserProfileQuery : IRequest<UserProfileResponse>;

public sealed class GetCurrentUserProfileQueryValidator : AbstractValidator<GetCurrentUserProfileQuery>
{
    public GetCurrentUserProfileQueryValidator()
    {
    }
}

public sealed class GetCurrentUserProfileHandler : IRequestHandler<GetCurrentUserProfileQuery, UserProfileResponse>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;

    public GetCurrentUserProfileHandler(ICurrentUserService currentUser, IIdentityService identity)
    {
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task<UserProfileResponse> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserIdOrThrow();
        var profile = await _identity.GetCurrentUserProfileAsync(userId, cancellationToken) ?? throw new NotFoundException("Current user profile could not be resolved.");
        return profile;
    }
}
