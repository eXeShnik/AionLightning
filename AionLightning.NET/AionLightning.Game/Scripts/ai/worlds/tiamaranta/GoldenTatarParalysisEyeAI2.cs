// GoldenTatarParalysisEyeAI2 — Java ai/worlds/tiamaranta/GoldenTatarParalysisEyeAI2.java. Paralysis
// hazard add: the "source" npc periodically spawns paralysis-tick adds; every other npc-id casts once
// and self-destructs.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("golden_tatar_paralysis_eye")]
public sealed class GoldenTatarParalysisEyeAI2 : AggressiveNpcAI2
{
    private int _spawnCount;

    public override void OnSpawned()
    {
        base.OnSpawned();
        if (Owner.Template.NpcId == 282744)
        {
            StartSpawnTask();
        }
        else
        {
            UseSkill(20213, 60);
            ScheduleTask(() =>
            {
                // note: Java called AI2Actions.deleteOwner(this) here; no owner-delete hook is exposed to
                // scripts yet.
            }, 2000);
        }
        // note: Java also overrode canThink() to return false; no equivalent hook exists on NpcAi2.
    }

    private void StartSpawnTask()
    {
        ScheduleTask(() =>
        {
            if (Owner.IsAlreadyDead)
            {
                CancelTasks();
                return;
            }
            _spawnCount++;
            Spawn(282745, Owner.Position.X, Owner.Position.Y, Owner.Position.Z, (byte)Owner.Position.Heading);
            if (_spawnCount >= 10)
            {
                CancelTasks();
                // note: Java then called AI2Actions.deleteOwner(this); no owner-delete hook is exposed to
                // scripts yet.
            }
        }, 14000, 2000);
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        CancelTasks();
        base.OnDied();
        // note: Java then called AI2Actions.deleteOwner(this); no owner-delete hook is exposed to scripts
        // yet. It also overrode ask(CAN_ATTACK_PLAYER/CAN_RESIST_ABNORMAL) to always allow both; no
        // AI-question poll hook exists on NpcAi2 yet.
    }
}
