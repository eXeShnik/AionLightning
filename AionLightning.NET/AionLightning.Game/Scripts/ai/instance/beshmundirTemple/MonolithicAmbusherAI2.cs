// MonolithicAmbusherAI2 — Java ai/instance/beshmundirTemple/MonolithicAmbusherAI2.java. Pure
// pass-through: neither override added behavior beyond the aggressive-root delegation.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("monolithicambusher")]
public sealed class MonolithicAmbusherAI2 : AggressiveNpcAI2
{
    public override void OnBackHome() => base.OnBackHome();

    public override void OnCreatureAggro(Creature creature) => base.OnCreatureAggro(creature);
}
