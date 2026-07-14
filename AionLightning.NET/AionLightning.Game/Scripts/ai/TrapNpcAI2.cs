// TrapNpcAI2 — Java ai/TrapNpcAI2.java. Ground trap that activates against hostile creatures
// entering its aggro range, then despawns after a delay.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("trap")]
public sealed class TrapNpcAI2 : NpcAi2
{
    public override void OnCreatureMoved(Creature creature)
    {
        // note: Java activated the trap (target + cast a random skill from its skill list, then despawn
        // after a delay) once a hostile creature entered its aggro range; AggroRange/skill-list/
        // creator-enemy checks aren't exposed to scripts yet (also overrode isMoveSupported to return
        // false and pollInstance to refuse decay/respawn/reward — neither has a C# equivalent).
    }

    public override void OnSpawned()
    {
        // note: Java refreshed its known-list and immediately tried to activate against everything already
        // in range (same logic as OnCreatureMoved).
        base.OnSpawned();
    }
}
