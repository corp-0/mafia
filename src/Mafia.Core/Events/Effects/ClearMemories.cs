using fennecs;
using Mafia.Core.Context;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Events.Effects.Interfaces;

namespace Mafia.Core.Events.Effects;

public sealed class ClearMemories(string rootPath, string targetPath) : IEventEffect
{
    public void Apply(EntityScope context)
    {
        if (!context.TryNavigate(rootPath, targetPath, out Entity root, out Entity target)) return;
        if (!root.Has<MemoriesOf>(target)) return;

        root.Ref<MemoriesOf>(target).Memories.Clear();
    }
}
