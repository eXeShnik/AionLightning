// CadellasHetgolemAI2 — Java ai/instance/argentManor/CadellasHetgolemAI2.java. Cadella's healer
// helper: walks toward her, casts a per-npc-id heal skill on arrival, then despawns itself.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("cadellas_hetgolem")]
public sealed class CadellasHetgolemAI2 : GeneralNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java set state FOLLOWING and, after 1s, cast a no-animation skill (19571) on itself.
        // canThink() always returned false (no C# equivalent) so its own think loop never ran.
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: Java aborted the move, shouted, cast a per-npc-id heal skill (19525-19529 depending on
        // which of the 5 helper spawns this is) on Cadella, then despawned itself after 4s via
        // AI2Actions.deleteOwner. Also overrode ask(CAN_RESIST_ABNORMAL=POSITIVE) and
        // pollInstance(SHOULD_DECAY/RESPAWN/REWARD=NEGATIVE) — neither hook exists on NpcAi2.
    }
}
