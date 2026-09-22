namespace ECommerce.Infrastructure.Identity;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ECommerce.Api";
    public string Audience { get; set; } = "ECommerce.Api.Clients";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 60;
}
