// IuAI2 — Java ai/instance/nightmareCircus/IuAI2.java. Periodically scans nearby players for a
// self-heal target and can apply a skill effect directly on request.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("iu")]
public sealed class IuAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        StartSchedule();
    }

    private void StartSchedule()
    {
        ScheduleTask(CheckForHeal, 10000);
    }

    private void CheckForHeal()
    {
        // note: Java scanned this npc's known-list for a player within 10m missing abnormal effect
        // 21363 and below max HP, then self-targeted and cast skill 21363 on it (AI2Actions +
        // SkillEngine); known-list enumeration and skill/effect application aren't wired at the
        // script layer yet.
        StartSchedule();
    }

    public void PlayerSkillUse(Player player, int skillId)
    {
        ApplyEffect(player, skillId);
    }

    private static void ApplyEffect(Creature creature, int skillId)
    {
        // note: Java applied a skill effect directly via SkillEngine.applyEffectDirectly; not wired at
        // the script layer yet.
        _ = creature;
        _ = skillId;
    }
}
