using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class ChangeRank(string path, RankId newRank) : IEventEffect, IDescribableEffect
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;
        if (!entity.Has<Rank>()) return;
        entity.Ref<Rank>() = new Rank(newRank);
    }

    public Localizable Describe(EntityScope context) =>
        new("effect.change_rank", new Dictionary<string, object?>
        {
            ["rank"] = newRank.ToString().ToLower()
        });
}
