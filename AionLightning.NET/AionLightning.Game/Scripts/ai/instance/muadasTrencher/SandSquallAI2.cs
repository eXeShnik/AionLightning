// SandSquallAI2 — Java ai/instance/muadasTrencher/SandSquallAI2.java. Timed sandstorm hazard:
// fires a scripted chain of self-cast skills over ~20s then self-deletes.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("sand_squall")]
public sealed class SandSquallAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        CastSkillAt(19896, 500);
        CastSkillAt(19894, 500);
        CastSkillAt(19894, 2500);
        CastSkillAt(20444, 4500);
        CastSkillAt(19894, 6500);
        CastSkillAt(19894, 8500);
        CastSkillAt(19894, 10500);
        CastSkillAt(20444, 12500);
        CastSkillAt(19894, 14500);
        CastSkillAt(19894, 16500);
        CastSkillAt(19895, 18500);
        ScheduleTask(() =>
        {
            // note: Java despawned the owner via AI2Actions.deleteOwner here; no scripted despawn API
            // exists yet (also overrode canThink()/ask()/pollInstance() to stay always-active, ignore
            // abnormals, and refuse decay/respawn/reward — the AI2 poll/gating framework has no C#
            // equivalent).
        }, 20000);
    }

    private void CastSkillAt(int skillId, int delayMs) =>
        ScheduleTask(() => { if (!Owner.IsAlreadyDead) UseSkill(skillId, 60); }, delayMs);

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java called AI2Actions.deleteOwner(this) here too; no scripted despawn API exists yet.
    }
}
