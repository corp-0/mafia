using fennecs;
using Mafia.Core.Content.Registries;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Systems;
using Mafia.Core.Events.Definition;
using Mafia.Core.Time;
using Microsoft.Extensions.Logging;

namespace Mafia.Core.Events.Engine;

/// <summary>
/// Central event pipeline. Collects candidates from trigger sources, applies
/// gating logic (one-time, cooldown, conditions, MTTH), and queues survivors
/// for presentation. AI events are resolved immediately; player events are
/// returned as <see cref="PendingPlayerEvent"/> for the UI layer to handle.
/// </summary>
public class EventOrchestrator(
    IEventDefinitionRepository definitions,
    EventQueue queue,
    MtthCalculator mtthCalculator,
    AiEventResolver aiResolver,
    IEventHistory history,
    TargetPoolResolver poolResolver,
    ILogger<EventOrchestrator> logger)
{
    private readonly List<IEventTriggerSource> _sources = [];

    /// <summary>
    /// Adds a trigger source that will be polled every tick for event candidates.
    /// </summary>
    public void RegisterTriggerSource(IEventTriggerSource source) => _sources.Add(source);

    /// <summary>
    /// Polls all registered trigger sources and runs each candidate through the
    /// gating pipeline (one-time, cooldown, conditions, MTTH). Survivors are
    /// placed into the <see cref="EventQueue"/> for later presentation.
    /// Call once per simulation tick.
    /// </summary>
    /// <param name="tickDurationDays">Length of this tick in game-days (used for MTTH probability).</param>
    /// <param name="currentDate">The current game date.</param>
    public void Tick(double tickDurationDays, GameDate currentDate)
    {
        queue.ResetTickCounter();

        foreach (var source in _sources)
        {
            foreach (var candidate in source.GetCandidates(currentDate))
            {
                ProcessCandidate(candidate, tickDurationDays, currentDate);
            }
        }
    }

    /// <summary>
    /// Dequeues events and presents them. AI-owned events are resolved immediately.
    /// The first player-owned event is returned for UI handling.
    /// Events are re-validated (alive check, conditions) before presentation.
    /// </summary>
    /// <param name="isAlive">Predicate that returns true if the root entity is still alive.</param>
    /// <param name="isPlayer">Predicate that returns true if the root entity is the player.</param>
    /// <returns>The next player event to display, or null if none remain.</returns>
    public PendingPlayerEvent? TryPresent(Func<Entity, bool> isAlive, Func<Entity, bool> isPlayer)
    {
        while (queue.CanPresentMore)
        {
            var queued = queue.Dequeue();
            if (queued is null) break;

            var def = queued.Definition;
            var scope = queued.Scope;
            var rootEntity = scope.ResolveAnchor("root");

            if (!IsStillValid(def, scope, rootEntity, isAlive))
                continue;

            if (rootEntity is { } root)
                history.Record(def.Id, root, queued.QueuedDate);

            scope.ChainedEventTriggered += OnChainedEventTriggered;

            if (rootEntity is { } playerRoot && isPlayer(playerRoot))
                return PresentToPlayer(def, scope);

            ResolveAsAi(def, scope);
        }

        return null;
    }

    /// <summary>
    /// Applies the outcome of the player's chosen option and unsubscribes
    /// the scope from chained event handling.
    /// Call after the UI collects the player's choice from a <see cref="PendingPlayerEvent"/>.
    /// </summary>
    /// <param name="definition">The event definition that was presented.</param>
    /// <param name="scope">The scope the event was presented in.</param>
    /// <param name="chosenOptionId">The ID of the option the player selected.</param>
    public void ResolvePlayerChoice(EventDefinition definition, EntityScope scope, string chosenOptionId)
    {
        var option = definition.Options.FirstOrDefault(o => o.Id == chosenOptionId);
        if (option is null) return;

        aiResolver.ApplyOptionOutcome(option, scope);
        scope.ChainedEventTriggered -= OnChainedEventTriggered;
    }

    private void ProcessCandidate(EventCandidate candidate, double tickDurationDays, GameDate currentDate)
    {
        var def = candidate.Definition;
        var scope = candidate.Scope;
        var rootEntity = scope.ResolveAnchor("root");

        if (IsBlockedByHistory(def, rootEntity, currentDate))
            return;

        if (def.Conditions is not null && !def.Conditions.Evaluate(scope))
            return;

        if (!PassesMtthRoll(def, scope, tickDurationDays))
            return;

        queue.Enqueue(new QualifiedEvent(def, scope, def.Priority, currentDate));
    }

    private bool IsBlockedByHistory(EventDefinition def, Entity? rootEntity, GameDate currentDate)
    {
        if (rootEntity is not { } root)
            return false;

        if (def.IsOneTimeOnly && history.HasFired(def.Id, root))
            return true;

        if (def.CooldownDays > 0)
        {
            var lastFired = history.LastFiredDate(def.Id, root);
            if (lastFired is not null && currentDate.DaysSince(lastFired.Value) < def.CooldownDays)
                return true;
        }

        return false;
    }

    private bool PassesMtthRoll(EventDefinition def, EntityScope scope, double tickDurationDays)
    {
        if (def is not PulseEventDefinition pulse)
            return true;

        var effectiveMtth = mtthCalculator.CalculateEffective(
            pulse.MeanTimeToHappenDays, pulse.MtthModifiers, scope);

        return mtthCalculator.Roll(effectiveMtth, tickDurationDays);
    }

    private static bool IsStillValid(EventDefinition def, EntityScope scope, Entity? rootEntity, Func<Entity, bool> isAlive)
    {
        if (rootEntity is { } root && !isAlive(root))
            return false;

        return def.Conditions is null || def.Conditions.Evaluate(scope);
    }

    private static PendingPlayerEvent PresentToPlayer(EventDefinition def, EntityScope scope)
    {
        return new PendingPlayerEvent(def, scope, def.GetVisibleOptions(scope));
    }

    private void ResolveAsAi(EventDefinition def, EntityScope scope)
    {
        aiResolver.Resolve(def, scope);
        scope.ChainedEventTriggered -= OnChainedEventTriggered;
    }

    private void OnChainedEventTriggered(string eventId, EntityScope? newScope)
    {
        var chainedDef = definitions.GetById(eventId);
        if (chainedDef is null) return;

        var scope = newScope ?? throw new InvalidOperationException(
            $"Chained event '{eventId}' triggered without a scope. " +
            "Use TriggerEvent with a scope or ensure the original scope propagates.");

        if (chainedDef.TargetSelection is { } selection)
        {
            var root = scope.ResolveAnchor("root");
            if (root is not { } rootEntity) return;

            var target = poolResolver.ResolveTarget(selection, rootEntity, scope);
            if (target is null) return;

            scope.WithAnchor("target", target.Value);
        }

        queue.Enqueue(new QualifiedEvent(chainedDef, scope, chainedDef.Priority, scope.CurrentDate));
    }
}
