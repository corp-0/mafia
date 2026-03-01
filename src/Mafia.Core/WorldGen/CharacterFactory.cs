using fennecs;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Components.State;
using Mafia.Core.Ecs.Components.Tags;

namespace Mafia.Core.WorldGen;

public class CharacterFactory(World world)
{
    public Entity Spawn(CharacterBlueprint blueprint)
    {
        Entity entity = world.Spawn()
            .Add<Character>()
            .Add(new CharacterName(blueprint.Name, blueprint.NickName))
            .Add(new Age { Amount = blueprint.Age })
            .Add(blueprint.Sex)
            .Add(new Muscle(blueprint.Muscle))
            .Add(new Nerve(blueprint.Nerve))
            .Add(new Brains(blueprint.Brains))
            .Add(new Charm(blueprint.Charm))
            .Add(new Instinct(blueprint.Instinct))
            .Add(new Wealth { Amount = blueprint.Wealth })
            .Add<Stress>()
            .Add<Notoriety>();
        
        if (blueprint.Rank.HasValue)
            entity.Add(new Rank(blueprint.Rank.Value));

        if (!string.IsNullOrEmpty(blueprint.Surname))
            entity.Add(new Surname(blueprint.Surname));

        return entity;
    }
}
