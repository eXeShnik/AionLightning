// SanctuaryCannon1AI2 — Java ai/instance/danuarSanctuary/SanctuaryCannon1AI2.java. 2nd-floor cannon
// prop: used to silently kill the door NPC guarding the 3rd floor.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("sanctuary_cannon_1")]
public sealed class SanctuaryCannon1AI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java looked up map 301140000's WorldMapInstance and silently killed every npc 730866 in it
        // via WorldMapInstance.getNpcs/AI2Actions.killSilently; instance-scoped multi-npc lookup by id and
        // silent-kill aren't exposed to scripts yet.
    }
}
