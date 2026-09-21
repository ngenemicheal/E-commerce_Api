using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.MongoDb;
using ECommerce.Infrastructure.Persistence.MongoDb.Repositories;
using ECommerce.Infrastructure.Persistence.Repositories;
using ECommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace ECommerce.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration.GetValue<string>("Database:Provider") ?? "Postgres";

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ECommerceDbContext>(options =>
            options.UseNpgsql(connectionString, o =>
                o.MigrationsAssembly(typeof(ECommerceDbContext).Assembly.FullName)));

        services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ECommerceDbContext>();

        services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserIdClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub;
            options.ClaimsIdentity.SecurityStampClaimType = "ss";
        });

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        if (string.Equals(dbProvider, "Mongo", StringComparison.OrdinalIgnoreCase))
        {
            MongoDbClassMaps.RegisterAll();
            services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
            var mongoOptions = configuration.GetSection(MongoDbOptions.SectionName).Get<MongoDbOptions>()
                ?? throw new InvalidOperationException($"Section '{MongoDbOptions.SectionName}' not found.");
            if (string.IsNullOrWhiteSpace(mongoOptions.ConnectionString))
            {
                throw new InvalidOperationException("MongoDb ConnectionString is not configured.");
            }

            services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoOptions.ConnectionString));
            services.AddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoOptions.DatabaseName));

            services.AddScoped<ICategoryRepository, MongoCategoryRepository>();
            services.AddScoped<IProductRepository, MongoProductRepository>();
            services.AddScoped<ICartRepository, MongoCartRepository>();
            services.AddScoped<IOrderRepository, MongoOrderRepository>();
            services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
        }
        else
        {
            services.AddScoped<ICategoryRepository, EfCategoryRepository>();
            services.AddScoped<IProductRepository, EfProductRepository>();
            services.AddScoped<ICartRepository, EfCartRepository>();
            services.AddScoped<IOrderRepository, EfOrderRepository>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        }

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}
