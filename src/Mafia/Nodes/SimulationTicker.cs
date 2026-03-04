using fennecs;
using Godot;
using Mafia.Core;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Components.Tags;
using Mafia.Core.Ecs.Systems;
using Mafia.Core.Events.Engine;
using Mafia.Core.Time;
using Microsoft.Extensions.Logging;

namespace Mafia.Nodes;

[GlobalClass]
public partial class SimulationTicker : Node
{
    private const double TICK_DURATION_DAYS = 1.0 / 24.0;
    private const int MAX_FAST_FORWARD_TICKS_PER_FRAME = 2_400; // ~100 days

    private GameClock _clock = null!;
    private GameState _gameState = null!;
    private EventOrchestrator _orchestrator = null!;
    private IActionTrigger _actionTrigger = null!;
    private ITickSystem[] _dailySystems = null!;
    private ILogger _logger = null!;
    private bool _fastForwarding;
    private PendingPlayerEvent? _pendingEvent;

    [Signal]
    public delegate void DayAdvancedEventHandler(int year, int month, int day);

    [Signal]
    public delegate void PlayerEventPendingEventHandler();

    [Signal]
    public delegate void FastForwardCompletedEventHandler();

    [Signal]
    public delegate void SpeedChangedEventHandler(int newSpeed);

    public SimulationSpeed Speed
    {
        get => _clock.Speed;
        set
        {
            _clock.Speed = value;
            _clock.ResetAccumulator();
            EmitSignal(SignalName.SpeedChanged, (int)value);
        }
    }

    public GameDate CurrentDate => _gameState.CurrentDate;
    public PendingPlayerEvent? PendingEvent => _pendingEvent;

    public void Initialize()
    {
        _logger = GameServices.Get<ILoggerFactory>().CreateLogger<SimulationTicker>();
        _gameState = GameServices.Get<GameState>();
        _clock = GameServices.Get<GameClock>();

        _orchestrator = GameServices.Get<EventOrchestrator>();
        _actionTrigger = GameServices.Get<IActionTrigger>();

        var actionTrigger = GameServices.Get<ActionTrigger>();
        _orchestrator.RegisterTriggerSource(GameServices.Get<PulseTrigger>());
        _orchestrator.RegisterTriggerSource(GameServices.Get<StoryBeatTrigger>());
        _orchestrator.RegisterTriggerSource(actionTrigger);

        _dailySystems =
        [
            GameServices.Get<AgingSystem>(),
            GameServices.Get<RoutineExpenseSystem>(),
            GameServices.Get<ExpenseSettlementSystem>(),
            GameServices.Get<MemoryExpirationSystem>(),
        ];

        _logger.LogInformation("Initialized at {CurrentDate}", _gameState.CurrentDate);
    }

    public override void _Process(double delta)
    {
        if (_pendingEvent is not null)
            return;

        if (_fastForwarding)
        {
            ProcessFastForward();
            return;
        }

        var ticks = _clock.Advance(delta);
        for (var i = 0; i < ticks; i++)
        {
            if (AdvanceOneHour())
                return;
        }
    }

    public void StartFastForward()
    {
        _fastForwarding = true;
        _logger.LogInformation("Fast-forward started");
    }

    public void StopFastForward()
    {
        _fastForwarding = false;
        _clock.ResetAccumulator();
        EmitSignal(SignalName.FastForwardCompleted);
        _logger.LogInformation("Fast-forward stopped");
    }

    public void ResolvePlayerEvent(string chosenOptionId)
    {
        if (_pendingEvent is null) return;

        _orchestrator.ResolvePlayerChoice(
            _pendingEvent.Definition, _pendingEvent.Scope, chosenOptionId);
        _pendingEvent = null;
    }

    private void ProcessFastForward()
    {
        for (var i = 0; i < MAX_FAST_FORWARD_TICKS_PER_FRAME; i++)
        {
            if (AdvanceOneHour())
            {
                _fastForwarding = false;
                EmitSignal(SignalName.FastForwardCompleted);
                _logger.LogInformation("Fast-forward ended at {CurrentDate} (player event)", _gameState.CurrentDate);
                return;
            }
        }
    }

    /// <summary>
    /// Advances the simulation by one hour.
    /// Returns true if a player event was triggered (simulation should pause).
    /// </summary>
    private bool AdvanceOneHour()
    {
        _gameState.CurrentDate = _gameState.CurrentDate.AddHours(1);
        var date = _gameState.CurrentDate;

        // Daily systems run once per day at the start of each new day
        if (date.Hour == 0)
        {
            foreach (var system in _dailySystems)
                system.Tick(date, _actionTrigger);

            _logger.LogInformation("Day: {CurrentDate}", date);
            EmitSignal(SignalName.DayAdvanced, date.Year, date.Month, date.Day);
        }

        // Event orchestrator ticks every hour
        _orchestrator.Tick(TICK_DURATION_DAYS, date);

        var pending = _orchestrator.TryPresent(IsAlive, IsPlayer);
        if (pending is null)
            return false;

        _pendingEvent = pending;
        Speed = SimulationSpeed.Paused;
        EmitSignal(SignalName.PlayerEventPending);
        _logger.LogInformation("Event presented to the player");
        return true;
    }

    private static bool IsAlive(Entity entity) => !entity.Has<Killed>();
    private static bool IsPlayer(Entity entity) => entity.Has<PlayerControlled>();
}
