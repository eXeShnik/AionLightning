// SiegeDrillAI2 — Java ai/instance/danuarSanctuary/SiegeDrillAI2.java. Drill prop: used to silently
// kill a door NPC.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("siegedrill")]
public sealed class SiegeDrillAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java looked up map 301140000's WorldMapInstance and silently killed every npc 233189 in it
        // via WorldMapInstance.getNpcs/AI2Actions.killSilently; instance-scoped multi-npc lookup by id and
        // silent-kill aren't exposed to scripts yet.
    }
}
