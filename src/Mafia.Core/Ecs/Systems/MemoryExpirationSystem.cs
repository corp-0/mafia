using fennecs;
using Mafia.Core.Ecs.Relations;
using Mafia.Core.Opinions;
using Mafia.Core.Time;

namespace Mafia.Core.Ecs.Systems;

public class MemoryExpirationSystem(World world) : ITickSystem
{
    private readonly Stream<MemoriesOf> _stream = world.Query<MemoriesOf>().Stream();

    public void Tick(GameDate currentDate)
    {
        foreach ((Entity _, MemoriesOf memories) in _stream)
        {
            memories.Memories.RemoveAll(m => currentDate >= m.ExpiresOn);
        }
    }
}
