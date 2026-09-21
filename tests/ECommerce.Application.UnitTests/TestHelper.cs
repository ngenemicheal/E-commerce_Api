using System.Reflection;
using ECommerce.Application.Extensions;
using ECommerce.Application.Interfaces;
using Mapster;
using MapsterMapper;
using NSubstitute;

namespace ECommerce.Application.UnitTests;

public static class TestHelper
{
    public static IMapper CreateMapper()
    {
        var config = TypeAdapterConfig.GlobalSettings.Clone();
        config.Scan(Assembly.GetAssembly(typeof(ApplicationServiceCollectionExtensions))!);
        config.RequireExplicitMapping = false;
        config.RequireDestinationMemberSource = false;
        config.Compile();
        return new Mapper(config);
    }

    public static IDateTimeProvider CreateDateTimeProvider(DateTimeOffset? now = null)
    {
        var time = now ?? DateTimeOffset.UtcNow;
        var provider = Substitute.For<IDateTimeProvider>();
        provider.UtcNow.Returns(time.UtcDateTime);
        provider.UtcNowOffset.Returns(time);
        return provider;
    }

    public static ICurrentUserService CreateCurrentUser(Guid? userId = null, string? email = null, params string[] roles)
    {
        var id = userId ?? Guid.NewGuid();
        var svc = Substitute.For<ICurrentUserService>();
        svc.UserId.Returns(id);
        svc.Email.Returns(email ?? $"user{id:N}@test.local");
        svc.Roles.Returns(roles.ToList().AsReadOnly());
        svc.IsAuthenticated.Returns(true);
        svc.IsInRole(Arg.Any<string>()).Returns(args => roles.Contains((string)args[0], StringComparer.OrdinalIgnoreCase));
        svc.GetUserIdOrThrow().Returns(id);
        return svc;
    }

    public static ICurrentUserService CreateAnonymousUser()
    {
        var svc = Substitute.For<ICurrentUserService>();
        svc.UserId.Returns((Guid?)null);
        svc.IsAuthenticated.Returns(false);
        svc.When(x => x.GetUserIdOrThrow()).Do(_ => throw new UnauthorizedAccessException());
        return svc;
    }
}
