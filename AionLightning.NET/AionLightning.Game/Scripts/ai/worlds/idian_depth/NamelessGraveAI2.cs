// NamelessGraveAI2 — Java ai/worlds/idian_depth/NamelessGraveAI2.java. Spawns a replacement npc at
// its own original spawn point on death.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("namelessgrave")]
public sealed class NamelessGraveAI2 : NpcAi2
{
    public override void OnDied()
    {
        base.OnDied();
        int spawnNpc = Owner.Template.NpcId switch
        {
            230984 => 283905,
            _ => 0,
        };
        var home = Owner.HomePosition;
        Spawn(spawnNpc, home.X, home.Y, home.Z, (byte)home.Heading);
    }
}
