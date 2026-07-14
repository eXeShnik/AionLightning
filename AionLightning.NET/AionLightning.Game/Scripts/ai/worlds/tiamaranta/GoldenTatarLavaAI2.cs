// GoldenTatarLavaAI2 — Java ai/worlds/tiamaranta/GoldenTatarLavaAI2.java. Lava hazard add: the
// "source" npc periodically spawns damage-tick adds; every other npc-id casts once and self-destructs.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("golden_tatar_lava")]
public sealed class GoldenTatarLavaAI2 : AggressiveNpcAI2
{
    private int _spawnCount;

    public override void OnSpawned()
    {
        base.OnSpawned();
        if (Owner.Template.NpcId == 282746)
        {
            StartSpawnTask();
        }
        else
        {
            ScheduleTask(() =>
            {
                if (Owner.IsAlreadyDead) return;
                UseSkill(20215, 60);
                // note: Java then called AI2Actions.deleteOwner(this); no owner-delete hook is exposed to
                // scripts yet.
            }, 500);
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
            Spawn(282747, Owner.Position.X, Owner.Position.Y, Owner.Position.Z, (byte)Owner.Position.Heading);
            if (_spawnCount >= 20)
            {
                CancelTasks();
                // note: Java then called AI2Actions.deleteOwner(this); no owner-delete hook is exposed to
                // scripts yet.
            }
        }, 3000, 3000);
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
        // yet. It also overrode ask(CAN_ATTACK_PLAYER) to always allow players as targets; no AI-question
        // poll hook exists on NpcAi2 yet.
    }
}
