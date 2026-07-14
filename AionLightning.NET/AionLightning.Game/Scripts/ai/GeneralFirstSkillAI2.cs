// GeneralFirstSkillAI2 — Java ai/GeneralFirstSkillAI2.java. General NPC that re-casts its spawn
// skill (SkillList.getUseInSpawnedSkill()) on spawn/respawn/back-home.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("general_first_skill")]
public sealed class GeneralFirstSkillAI2 : GeneralNpcAI2
{
    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java re-cast getSkillList().getUseInSpawnedSkill() via SkillEngine on backHome/respawn/
        // spawn; NPC skill lists and SkillEngine aren't exposed to scripts yet (see NpcAi2.UseSkill).
        // handleRespawned had no ported hook (dropped — no C# equivalent).
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: see OnBackHome — same UseInSpawnedSkill re-cast, dropped for the same reason.
    }
}
