// LordVallakhanAI2 — Java ai/instance/udasTemple/LordVallakhanAI2.java. Lord Vallakhan boss:
// HP-threshold add-spawns plus a repeating "Einsturz" skill-use/re-target sequence while engaged.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("lordvallakhan")]
public sealed class LordVallakhanAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java shouted message 1500022 and started a repeating 60s "Einsturz" task on the first
        // attack after spawn/back-home (each tick: shout 1500023, a 2s AI2Actions.useSkill(18599) walking
        // step, then an 8.5s re-target step via AggroList/getMoveController/getGameStats), and at 99/75/30/
        // 10% HP spawned tracked helper adds (281384/281524) via SpawnEngine; none of that choreography is
        // wired at the script layer yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java despawned tracked helper adds and cancelled the Einsturz task here.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java despawned tracked helper adds, cancelled the Einsturz task, and shouted message
        // 1500024 on death.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        // note: Java despawned tracked helper adds here too.
    }
}
