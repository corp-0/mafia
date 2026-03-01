using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Extensions;
using Mafia.Core.Text;

namespace Mafia.Core.Events.Effects;

public class ChangeNickname(string path, string newNickname) : IEventEffect, IDescribableEffect
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(path, out Entity entity)) return;
        entity.ModifyComponent<CharacterName>(cn => cn with { NickName = newNickname });
    }

    public Localizable Describe(EntityScope context) =>
        new("effect.change_nickname", new Dictionary<string, object?>
        {
            ["nickname"] = newNickname
        });
}
