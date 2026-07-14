// InfiltratorsAI2 — Java ai/quests/InfiltratorsAI2.java. Infiltrator quest chain NPC: spawns its
// next-stage replacement at its own spawn point on death.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("infiltrator")]
public sealed class InfiltratorsAI2 : AggressiveNpcAI2
{
    public override void OnDied()
    {
        base.OnDied();
        int spawnNpc = Owner.Template.NpcId switch
        {
            282913 => 282914,
            282918 => 282920,
            282920 => 282922,
            282917 => 282915,
            282915 => 282916,
            282919 => 282921,
            282921 => 282923,
            _ => 0,
        };
        if (spawnNpc != 0)
        {
            var home = Owner.HomePosition;
            Spawn(spawnNpc, home.X, home.Y, home.Z, (byte)home.Heading);
        }
    }
}
