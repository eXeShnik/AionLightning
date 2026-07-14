// ShulackGuidedBombAI2 — Java ai/instance/aturamSkyFortress/ShulackGuidedBombAI2.java. Guided bomb
// prop: self-destructs after 10s unless it detonates first by staying within 4m of its aggressor.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("shulack_guided_bomb")]
public sealed class ShulackGuidedBombAI2 : AggressiveNpcAI2
{
    public override void OnDespawned()
    {
        base.OnDespawned();
        // note: Java cancelled its 1s-repeating proximity-check task here.
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java scheduled a 10s self-destruct (AI2Actions.deleteOwner) unless it had already
        // detonated.
    }

    public override void OnCreatureAggro(Creature creature)
    {
        base.OnCreatureAggro(creature);
        // note: Java started a 1s-repeating task that, once the aggressor stayed within 4m, cast skill
        // 19415 and self-destructed after 3.2s. MathUtil distance checks aren't exposed to scripts yet.
        // Also overrode ask(CAN_RESIST_ABNORMAL=POSITIVE) and pollInstance(SHOULD_DECAY/RESPAWN/
        // REWARD=NEGATIVE) — neither poll hook exists on NpcAi2.
    }
}
