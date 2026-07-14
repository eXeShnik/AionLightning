// GuardTowerAI2 — Java ai/siege/GuardTowerAI2.java. Siege guard tower: widens its attack range,
// then auto-targets and repeatedly casts a random skill at any player that wanders within it.
// note: Java also overrode handleCreatureNotSee (clears its running attack task) — no CreatureNotSee
// hook exists in NpcAi2.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("guardtower")]
public sealed class GuardTowerAI2 : NpcAi2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: Java widened its NpcObjectTemplate attack range to 30; NpcTemplate has no mutable
        // attack-range field at the script layer.
    }

    public override void OnCreatureSee(Creature creature) => CheckDistance(creature);

    public override void OnCreatureMoved(Creature creature) => CheckDistance(creature);

    private void CheckDistance(Creature creature)
    {
        // note: Java targeted and started a repeating (1s delay, 8s period) random-skill-use task against
        // any Player within 30m (MathUtil.isIn3dRange), cancelling it once the target died. MathUtil range
        // checks, AI2Actions.targetCreature, and the NPC skill-list/TaskId scheduling aren't exposed to the
        // script layer yet.
    }
}
