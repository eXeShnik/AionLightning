// MagicalSapAI2 — Java ai/instance/argentManor/MagicalSapAI2.java. One-shot environmental prop:
// fires a no-animation skill 4 times on a stagger, then deletes itself.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("magical_sap")]
public sealed class MagicalSapAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java scheduled a no-animation skill (19306) at 1s/4s/7s/10s after spawn, deleting itself
        // via AI2Actions.deleteOwner on the last one. SkillEngine casting and AI2Actions aren't exposed
        // to scripts yet.
    }
}
