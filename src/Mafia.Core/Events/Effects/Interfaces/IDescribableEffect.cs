using Mafia.Core.Context;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects.Interfaces;

public interface IDescribableEffect
{
    Localizable Describe(EntityScope context);
}