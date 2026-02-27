using Microsoft.Extensions.DependencyInjection;

namespace Mafia.Core;

public static class CoreStartup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register your core services here
        // services.AddSingleton<SomeService>();
        // services.AddScoped<AnotherService>();

        // Example:
        // services.AddSingleton<IWorld>(new World());
    }
}
