namespace Mafia.Core.Ecs.Components.State;

public interface IDisableReason;

public record struct Disabled(int Count);
public record struct Killed : IDisableReason;
public record struct Arrested : IDisableReason;
public record struct Fled : IDisableReason;
