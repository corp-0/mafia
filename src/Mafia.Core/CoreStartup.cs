using Mafia.Core.Content;
using Mafia.Core.Content.Registries;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Mafia.Core;

public static class CoreStartup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IEventDefinitionRepository, EventDefinitionRepository>();
        services.AddSingleton<IEventHistory, EventHistory>();
        services.AddSingleton<EventQueue>();
        services.AddSingleton<MtthCalculator>();
        services.AddSingleton<AiEventResolver>();
        services.AddSingleton<EventOrchestrator>();
        services.AddSingleton<TargetPoolResolver>();
        services.AddSingleton<PulseTrigger>();
        services.AddSingleton<StoryBeatTrigger>();
        services.AddSingleton<ContentLoader>();
    }
}
