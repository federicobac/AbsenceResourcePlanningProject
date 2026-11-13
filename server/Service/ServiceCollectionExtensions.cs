using Microsoft.Extensions.DependencyInjection;
using Service.Security;

namespace Service;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceLayer(
        this IServiceCollection services,
        JwtSettings jwtSettings)
    {
        // Configuration
        services.AddSingleton(jwtSettings);
        
        // Security
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        
        // Application services
        services.AddScoped<UserService>();

        return services;
    }
}