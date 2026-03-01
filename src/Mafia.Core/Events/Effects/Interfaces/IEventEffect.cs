using Mafia.Core.Context;

namespace Mafia.Core.Events.Effects.Interfaces;

public interface IEventEffect
{
    void Apply(EntityScope context);
}