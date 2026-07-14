using System.Linq;
using System.Collections.Generic;
using System;
// UnstableRukrilAI2 — Java ai/instance/unstableSplinterpath/UnstableRukrilAI2.java. Boss: once
// below 95% HP, starts a repeating skill cast that tops up its worm add, and regens near Ebonsoul.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("unstablerukril")]
public sealed class UnstableRukrilAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
        Regen();
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 95 && _isHome)
        {
            _isHome = false;
            StartSkillTask();
        }
    }

    private void StartSkillTask()
    {
        var ebonsoul = GetNpc(219940);
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead)
            {
                CancelTasks();
            }
            else
            {
                if (GetNpc(284022) is null)
                {
                    UseSkill(19266);
                    Spawn(284022, Owner.Position.X + 2, Owner.Position.Y - 2, Owner.Position.Z);
                }
                if (ebonsoul is not null && !ebonsoul.IsAlreadyDead)
                {
                    // note: Java cast skill 19159 as Ebonsoul itself via SkillEngine; UseSkill only casts
                    // as this AI's own owner, so Ebonsoul's self-cast can't be reproduced here.
                    Spawn(284023, ebonsoul.Position.X + 2, ebonsoul.Position.Y - 2, ebonsoul.Position.Z);
                }
            }
        }, 5000, 70000);
    }

    private void Regen()
    {
        var ebonsoul = GetNpc(219940);
        if (ebonsoul is not null && !ebonsoul.IsAlreadyDead && Owner.Position.DistanceTo(ebonsoul.Position) <= 5)
        {
            if (Owner.CurrentHp < Owner.MaxHp)
                Owner.CurrentHp = Math.Min(Owner.MaxHp, Owner.CurrentHp + 10000);
        }
    }

    public override void OnDied()
    {
        base.OnDied();
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        CancelTasks();
        _isHome = true;
        Owner.RemoveEffectBySkillId(19266);
    }
}
