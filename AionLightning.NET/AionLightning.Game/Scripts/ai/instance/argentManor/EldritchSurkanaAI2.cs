// EldritchSurkanaAI2 — Java ai/instance/argentManor/EldritchSurkanaAI2.java. Trash-add helper that
// deletes itself on death.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("eldritch_surkana")]
public sealed class EldritchSurkanaAI2 : NpcAi2
{
    public override void OnDied()
    {
        base.OnDied();
        // note: Java deleted itself via AI2Actions.deleteOwner after the base death handling; also
        // overrode ask(CAN_RESIST_ABNORMAL=POSITIVE) — that poll hook doesn't exist on NpcAi2.
    }
}
