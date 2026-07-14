// GaleCycloneAI2 — Java ai/worlds/pernon/GaleCycloneAI2.java. Registers a per-player move observer
// that casts a knockback skill whenever the observed player moves.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("gale_cyclone")]
public sealed class GaleCycloneAI2 : NpcAi2
{
    public override void OnCreatureSee(Creature creature)
    {
        // note: Java registered a GaleCycloneObserver on any player it saw, casting skill 20528 on their
        // every move; observer registration (player.getObserveController()) isn't exposed to scripts yet.
        // Java's handleCreatureNotSee (unregistering the observer) also has no C# equivalent hook.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java cleared every registered observer here (see OnCreatureSee).
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        // note: same observer cleanup as OnDied.
    }
}
