using Mafia.Core.Content;
using Mafia.Core.Content.Registries;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Ecs.Systems;
using Mafia.Core.Events.Engine;
using Mafia.Core.Time;
using Microsoft.Extensions.DependencyInjection;

namespace Mafia.Core;

public static class CoreStartup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Content
        services.AddSingleton<IOpinionRuleRepository, OpinionRuleRepository>();
        services.AddSingleton<IEventDefinitionRepository, EventDefinitionRepository>();
        services.AddSingleton<ContentMetadataStore>();
        services.AddSingleton<NameRepository>();
        services.AddSingleton<INameRepository>(sp => sp.GetRequiredService<NameRepository>());
        services.AddSingleton<ContentLoader>();

        // Event engine
        services.AddSingleton<IEventHistory, EventHistory>();
        services.AddSingleton<EventQueue>();
        services.AddSingleton<MtthCalculator>();
        services.AddSingleton<AiEventResolver>();
        services.AddSingleton<EventOrchestrator>();
        services.AddSingleton<TargetPoolResolver>();
        services.AddSingleton<PulseTrigger>();
        services.AddSingleton<StoryBeatTrigger>();
        services.AddSingleton<ActionTrigger>();
        services.AddSingleton<IActionTrigger>(sp => sp.GetRequiredService<ActionTrigger>());

        // Tick systems
        services.AddSingleton<AgingSystem>();
        services.AddSingleton<RoutineExpenseSystem>();
        services.AddSingleton<ExpenseSettlementSystem>();
        services.AddSingleton<MemoryExpirationSystem>();

        // Time (GameState registered externally with start date, World registered externally)
        services.AddSingleton<GameClock>();
    }
}
