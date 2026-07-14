// RM1337AI2 — Java ai/instance/empyreanCrucible/RM1337AI2.java. Empyrean Crucible boss: a repeating
// skill barrage from first attack, plus a once-only enrage barrage and spark adds below 75% HP.
using System;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("rm_1337")]
public sealed class RM1337AI2 : AggressiveNpcAI2
{
    private bool _isHome = true;
    private bool _isEventStarted;

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java shouted 1500229 via NpcShoutsService here; NPC shouts aren't exposed to scripts yet.
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        CancelTasks();
        // note: Java shouted 1500231 via NpcShoutsService here; NPC shouts aren't exposed to scripts yet.
        base.OnDied();
    }

    public override void OnBackHome()
    {
        CancelTasks();
        base.OnBackHome();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            StartSkillTask1();
        }
        if (getOwner().HpPercentage <= 75 && !_isEventStarted)
        {
            _isEventStarted = true;
            StartSkillTask2();
        }
    }

    private void StartSkillTask1()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead)
            {
                CancelTasks();
                return;
            }
            // note: Java skipped this tick entirely while the owner had an active cast in progress
            // (getCastingSkill() != null); casting-state isn't exposed to scripts yet.
            if (getOwner().HpPercentage <= 50)
            {
                if (Random.Shared.Next(2) == 0)
                {
                    UseSkill(19550, 10);
                }
                else
                {
                    UseSkill(19552, 10);
                    ScheduleTask(() => UseSkill(19553, 10), 4000);
                }
            }
            else
            {
                UseSkill(19550, 10);
            }
        }, 10000, 23000);
    }

    private void StartSkillTask2()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead)
            {
                CancelTasks();
                return;
            }
            // note: Java cancelled the owner's current skill cast here; skill-cancellation isn't exposed
            // to scripts yet. Also shouted 1500230 via NpcShoutsService.
            UseSkill(19551, 10);
            SpawnSparks();
        }, 0, 60000);
    }

    private void SpawnSparks()
    {
        ScheduleTask(() =>
        {
            if (getOwner().IsAlreadyDead) return;
            int count = Random.Shared.Next(8, 13);
            for (int i = 0; i < count; i++)
                RndSpawn(282373);
        }, 4000);
    }

    private void RndSpawn(int npcId)
    {
        double direction = Random.Shared.Next(0, 181) / 100.0;
        int distance = Random.Shared.Next(3, 13);
        float x1 = (float)(Math.Cos(Math.PI * direction) * distance);
        float y1 = (float)(Math.Sin(Math.PI * direction) * distance);
        var p = getOwner().Position;
        Spawn(npcId, p.X + x1, p.Y + y1, p.Z, 0);
    }
}
