using Microsoft.Extensions.DependencyInjection;

namespace Mafia.Core;

public static class GameServices
{
    private static IServiceProvider? _serviceProvider;

    public static void Initialize(IServiceCollection services)
    {
        _serviceProvider = services.BuildServiceProvider();
    }

    public static T Get<T>() where T : notnull
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("GameServices has not been initialized. Call Initialize() first.");
        }
        return _serviceProvider.GetRequiredService<T>();
    }
}
