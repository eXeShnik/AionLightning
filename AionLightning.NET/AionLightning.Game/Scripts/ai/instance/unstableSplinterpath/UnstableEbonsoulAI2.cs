using System.Linq;
using System.Collections.Generic;
using System;
// UnstableEbonsoulAI2 — Java ai/instance/unstableSplinterpath/UnstableEbonsoulAI2.java. Boss: once
// below 95% HP, starts a repeating skill cast that tops up its worm add, and regens near Rukril.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("unstableebonsoul")]
public sealed class UnstableEbonsoulAI2 : AggressiveNpcAI2
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
        var rukril = GetNpc(219939);
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead)
            {
                CancelTasks();
            }
            else
            {
                if (GetNpc(284023) is null)
                {
                    UseSkill(19159);
                    Spawn(284023, Owner.Position.X + 2, Owner.Position.Y - 2, Owner.Position.Z);
                }
                if (rukril is not null && !rukril.IsAlreadyDead)
                {
                    // note: Java cast skill 19266 as Rukril itself via SkillEngine; UseSkill only casts
                    // as this AI's own owner, so Rukril's self-cast can't be reproduced here.
                    Spawn(284022, rukril.Position.X + 2, rukril.Position.Y - 2, rukril.Position.Z);
                }
            }
        }, 5000, 70000);
    }

    private void Regen()
    {
        var rukril = GetNpc(219939);
        if (rukril is not null && !rukril.IsAlreadyDead && Owner.Position.DistanceTo(rukril.Position) <= 5)
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
