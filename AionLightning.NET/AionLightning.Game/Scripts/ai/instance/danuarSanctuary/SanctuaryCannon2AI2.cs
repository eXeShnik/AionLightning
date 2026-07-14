// SanctuaryCannon2AI2 — Java ai/instance/danuarSanctuary/SanctuaryCannon2AI2.java. Mid-room cannon
// prop: used to silently kill the door NPC guarding the opposite side.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("sanctuary_cannon_2")]
public sealed class SanctuaryCannon2AI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java looked up map 301140000's WorldMapInstance and silently killed every npc 730865 in it
        // via WorldMapInstance.getNpcs/AI2Actions.killSilently; instance-scoped multi-npc lookup by id and
        // silent-kill aren't exposed to scripts yet.
    }
}
