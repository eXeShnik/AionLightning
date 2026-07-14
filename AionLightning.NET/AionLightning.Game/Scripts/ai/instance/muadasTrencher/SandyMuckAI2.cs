// SandyMuckAI2 — Java ai/instance/muadasTrencher/SandyMuckAI2.java. Timed hazard NPC: pulses a
// self-cast skill every 5s for 25s then self-deletes.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("sandy_muck")]
public sealed class SandyMuckAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleSkillPulse(500);
        ScheduleSkillPulse(5000);
        ScheduleSkillPulse(10000);
        ScheduleSkillPulse(15000);
        ScheduleSkillPulse(20000);
        ScheduleSkillPulse(25000);
        ScheduleTask(() =>
        {
            // note: Java despawned the owner via AI2Actions.deleteOwner here; no scripted despawn API
            // exists yet (also overrode canThink()/ask() to stay always-active and always attackable —
            // the AI2 poll/gating framework has no C# equivalent).
        }, 30000);
    }

    private void ScheduleSkillPulse(int delayMs) =>
        ScheduleTask(() => { if (!Owner.IsAlreadyDead) UseSkill(19900, 50); }, delayMs);

    public override void OnDied()
    {
        base.OnDied();
        // note: Java called AI2Actions.deleteOwner(this) here too; no scripted despawn API exists yet.
    }
}
