// ServantNpcAI2 — Java ai/ServantNpcAI2.java. Summoned servant/totem that doesn't think for
// itself and instead loops a heal/attack skill on its creator's target.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("servant")]
public sealed class ServantNpcAI2 : GeneralNpcAI2
{
    public override void OnThink()
    {
        // servants are not thinking (matches Java's empty think() override).
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java targeted its creator's current target after 200ms then looped a heal/attack skill
        // (getSkillList().getRandomSkill()) every 3s (totem) or 5s via ThreadPoolManager (also overrode
        // isMoveSupported to return false and pollInstance to refuse decay/respawn/reward — neither has a
        // C# equivalent). Creator-target resolution and skill lists aren't exposed to scripts yet.
    }
}
