// WalkingTalkingBombAI2 — Java ai/instance/steelRose/WalkingTalkingBombAI2.java. Timed bomb NPC:
// self-casts a skill 2s after spawn, then self-destructs 4s later.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("walkingtalkingbomb")]
public sealed class WalkingTalkingBombAI2 : AggressiveNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
            {
                UseSkill(19416, 49);
                ScheduleTask(() =>
                {
                    // note: Java despawned the owner via AI2Actions.deleteOwner here; no scripted
                    // despawn API exists yet (also overrode ask()/pollInstance() to force
                    // CAN_RESIST_ABNORMAL and refuse decay/respawn/reward — the AIQuestion poll
                    // framework has no C# equivalent).
                }, 4000);
            }
        }, 2000);
    }
}
