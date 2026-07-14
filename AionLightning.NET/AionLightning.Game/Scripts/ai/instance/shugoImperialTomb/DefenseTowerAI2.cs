// DefenseTowerAI2 — Java ai/instance/shugoImperialTomb/DefenseTowerAI2.java. Shugo Imperial Tomb
// defense tower: hp-percentage self-buffs plus a repeating attack skill; never decays/respawns/rewards.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("defensetower")]
// 831130, 831250, 831251
public sealed class DefenseTowerAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        // note: Java applied these as direct effects (SkillEngine.applyEffectDirectly), not cast skills;
        // UseSkill is the closest available stub.
        if (hpPercentage > 50 && hpPercentage <= 100) UseSkill(21097);
        if (hpPercentage > 25 && hpPercentage <= 50) UseSkill(21098);
        if (hpPercentage >= 0 && hpPercentage <= 25) UseSkill(21099);
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        UseSkill(21097);
        ScheduleTask(() => UseSkill(20954), 2000, 2000);
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnBackHome()
    {
        // note: Java's handleBackHome was empty and deliberately did not call super.handleBackHome();
        // preserved as a no-op here too.
    }

    // note: Java also overrode modifyDamage (clamp to 1), canThink (always false), and pollInstance
    // (refuse SHOULD_DECAY/SHOULD_RESPAWN/SHOULD_REWARD) — none of these hooks exist on NpcAi2 yet.
}
