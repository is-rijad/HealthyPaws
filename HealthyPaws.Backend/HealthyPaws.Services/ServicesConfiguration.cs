using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.Breed;
using HealthyPaws.Services.Device;
using HealthyPaws.Services.Dog;
using HealthyPaws.Services.User;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyPaws.Services;

public static class ServicesConfiguration
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        services.AddScoped<AuthorizationService>();
        services.AddScoped<TokenManager>();

        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IDogService, DogService>();
        services.AddScoped<IBreedService, BreedService>();
        services.AddScoped<IDeviceService, DeviceService>();
    }
}