using System.Linq;
using System.Collections.Generic;
using System;
// HyperionTurretAI2 — Java ai/instance/infinityShard/HyperionTurretAI2.java. Hyperion support
// turret: below 5% HP, casts a self-destruct skill on its target.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("hy_turret")]
public sealed class HyperionTurretAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    public void OwnerSkillUse(int skillId)
    {
        // note: Java deleted itself via AI2Actions.deleteOwner when skillId == 21201; no scripted
        // despawn API exists yet.
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 5)
            UseSkill(21201); // note: Java targeted getTarget(), not itself; UseSkill has no target slot.
    }
}
