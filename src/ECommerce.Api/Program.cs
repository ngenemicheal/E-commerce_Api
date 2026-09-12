using System.Reflection;
using ECommerce.Api.Fakes;
using ECommerce.Api.Middleware;
using ECommerce.Application.Extensions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var keysDir = Path.Combine(Path.GetTempPath(), "ecommerce-aspnet-keys");
Directory.CreateDirectory(keysDir);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysDir)).SetApplicationName("ECommerce");

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddApplicationServices();

builder.Services.AddScoped<ICategoryRepository, FakeCategoryRepository>();
builder.Services.AddScoped<IProductRepository, FakeProductRepository>();
builder.Services.AddScoped<ICartRepository, FakeCartRepository>();
builder.Services.AddScoped<IOrderRepository, FakeOrderRepository>();
builder.Services.AddScoped<IUnitOfWork, FakeUnitOfWork>();

builder.Services.AddSingleton<IDateTimeProvider, FakeDateTimeProvider>();
builder.Services.AddScoped<ICurrentUserService>(_ => new FakeCurrentUserService(
    isAuthenticated: true,
    isAdmin: false));
builder.Services.AddSingleton<IIdentityService, FakeIdentityService>();

builder.Services.AddAuthentication("Fake").AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>("Fake", _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
