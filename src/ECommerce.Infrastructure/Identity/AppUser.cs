using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Identity;

public class AppUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
