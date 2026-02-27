using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects.Interfaces;
using Mafia.Core.Opinions;

namespace Mafia.Core.Events.Effects;

public sealed class AddMemory(string rootPath, string targetPath, OpinionMemory memory): IEventEffect
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(rootPath, targetPath, out Entity root, out Entity target)) return;

        if (!root.Has<MemoriesOf>(target))
            root.Add(new MemoriesOf(target), target);

        root.Ref<MemoriesOf>(target).Memories.Add(memory);
    }
}