// FlameSmashAI2 — Java ai/instance/rentusBase/FlameSmashAI2.java. Ground-fire hazard: casts its skill
// shortly after spawning, then self-despawns after 7s.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("flame_smash")]
public sealed class FlameSmashAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() => UseSkill(Owner.Template.NpcId == 283008 ? 20540 : 20539), 500);
        ScheduleTask(Despawn, 7000);
    }

    private void Despawn()
    {
        if (Owner.IsAlreadyDead) return;
        // note: Java called AI2Actions.deleteOwner(this) here; scripted NPC delete isn't exposed to
        // scripts yet.
    }
}
