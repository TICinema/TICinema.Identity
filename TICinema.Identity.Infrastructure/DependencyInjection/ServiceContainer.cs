using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TICinema.Identity.Application.Interfaces;
using TICinema.Identity.Application.Interfaces.Repositories;
using TICinema.Identity.Application.Interfaces.Services;
using TICinema.Identity.Domain.Entities;
using TICinema.Identity.Infrastructure.Configurations;
using TICinema.Identity.Infrastructure.Persistence;
using TICinema.Identity.Infrastructure.Repositories.Account;
using TICinema.Identity.Infrastructure.Repositories.Authentication;
using TICinema.Identity.Infrastructure.Services;

namespace TICinema.Identity.Infrastructure.DependencyInjection
{
    public static class ServiceContainer
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
            IConfiguration config)
        {
            var connectionString = config.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = config.GetConnectionString("Redis");
                options.InstanceName = "TICinema_"; // Префикс для ключей
            });

            services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
            services.AddScoped<IJwtService, JwtService>();

            return services;
        }
    }
}