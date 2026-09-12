using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Extensions;

public static class DatabaseSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var scoped = scope.ServiceProvider;

        var context = scoped.GetRequiredService<ECommerceDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = scoped.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scoped.GetRequiredService<UserManager<AppUser>>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Enum.GetNames(typeof(Role)))
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<AppUser> userManager)
    {
        const string adminEmail = "admin@example.com";
        const string adminPassword = "Admin123!";

        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing is not null)
        {
            return;
        }

        var admin = new AppUser
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Email = adminEmail,
            UserName = adminEmail,
            FirstName = "System",
            LastName = "Administrator",
            CreatedAt = DateTimeOffset.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Failed to create admin user: " +
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRolesAsync(admin, new[] { Role.Customer.ToString(), Role.Admin.ToString() });
    }
}
