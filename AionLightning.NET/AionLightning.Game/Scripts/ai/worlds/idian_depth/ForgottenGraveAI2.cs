// ForgottenGraveAI2 — Java ai/worlds/idian_depth/ForgottenGraveAI2.java. Spawns a replacement npc at
// its own original spawn point on death.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("forgottengrave")]
public sealed class ForgottenGraveAI2 : NpcAi2
{
    public override void OnDied()
    {
        base.OnDied();
        int spawnNpc = Owner.Template.NpcId switch
        {
            230862 => 283906,
            _ => 0,
        };
        var home = Owner.HomePosition;
        Spawn(spawnNpc, home.X, home.Y, home.Z, (byte)home.Heading);
    }
}
