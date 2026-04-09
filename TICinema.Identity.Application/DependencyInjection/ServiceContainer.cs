using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TICinema.Identity.Application.Common.Mappings;
using TICinema.Identity.Application.Interfaces;
using TICinema.Identity.Application.Interfaces.Clients;
using TICinema.Identity.Application.Interfaces.Services;
using TICinema.Identity.Application.Services;
using TICinema.Identity.Application.Services.Clients;

namespace TICinema.Identity.Application.DependencyInjection
{
    public static class ServiceContainer
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => { }, typeof(MappingProfile));
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IUsersGrpcClient, UsersGrpcClient>();
            return services;
        }
    }
}
