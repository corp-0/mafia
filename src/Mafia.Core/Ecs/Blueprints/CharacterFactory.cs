using fennecs;
using Mafia.Core.Ecs.Components.Attributes;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Components.Rank;
using Mafia.Core.Ecs.Components.State;

namespace Mafia.Core.Ecs.Blueprints;

public class CharacterFactory(World world)
{
    public Entity Spawn(CharacterBlueprint blueprint)
    {
        var entity = world.Spawn();
        entity.Add<Character>();
        entity.Add(new CharacterName(blueprint.Name, blueprint.NickName));
        entity.Add(new Age { Amount = blueprint.Age });
        entity.Add(blueprint.Sex);
        if (blueprint.Rank.HasValue)
            entity.Add(new Rank(blueprint.Rank.Value));
        entity.Add(new Muscle(blueprint.Muscle));
        entity.Add(new Nerve(blueprint.Nerve));
        entity.Add(new Brains(blueprint.Brains));
        entity.Add(new Charm(blueprint.Charm));
        entity.Add(new Instinct(blueprint.Instinct));
        entity.Add(new Wealth { Amount = blueprint.Wealth });
        entity.Add<Stress>();
        entity.Add<Notoriety>();

        if (!string.IsNullOrEmpty(blueprint.Surname))
            entity.Add(new Components.Identity.Surname(blueprint.Surname));

        return entity;
    }
}
