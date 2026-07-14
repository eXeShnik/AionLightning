// PashidLabUnitTroublemakerAI2 — Java ai/instance/danuarMysticarium/PashidLabUnitTroublemakerAI2.java.
// Hidden ambusher: stays hidden and buffed until attacked, then reveals and clears its effects.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("pashid_lab_unit_troublemaker")]
public sealed class PashidLabUnitTroublemakerAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        if (Owner.IsAlreadyDead) return;
        UseSkill(19493);
        // note: Java also set CreatureVisualState.HIDE1 on the owner; visual-state flags aren't
        // exposed to the script layer yet.
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        Owner.RemoveEffectBySkillId(19493);
        // note: Java also cleared CreatureVisualState.HIDE1 on the owner; visual-state flags aren't
        // exposed to the script layer yet.
        Owner.ClearAllEffects();
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        if (Owner.IsAlreadyDead) return;
        UseSkill(19493);
        // note: Java also set CreatureVisualState.HIDE1 on the owner; visual-state flags aren't
        // exposed to the script layer yet.
    }
}
