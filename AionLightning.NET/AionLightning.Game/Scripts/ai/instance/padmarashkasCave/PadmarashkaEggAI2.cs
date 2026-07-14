// PadmarashkaEggAI2 — Java ai/instance/padmarashkasCave/PadmarashkaEggAI2.java. Padmarashka egg:
// spawns a guardian on first attack, then hatches into a different npc after a timed delay.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("padmarashkaegg")]
public sealed class PadmarashkaEggAI2 : NoActionAI2
{
    private const int SmallEggNpcId = 282613;
    private const int HugeEggNpcId = 282614;

    public override void OnSpawned()
    {
        base.OnSpawned();
        int hatchDelayMs = Owner.Template.NpcId switch
        {
            SmallEggNpcId => 60000,
            HugeEggNpcId => 120000,
            _ => 0,
        };
        if (hatchDelayMs == 0) return;
        int hatchNpcId = Owner.Template.NpcId == SmallEggNpcId ? 282616 : 282620;
        var pos = Owner.Position;
        ScheduleTask(() =>
        {
            if (!Owner.IsAlreadyDead)
                Spawn(hatchNpcId, pos.X, pos.Y, pos.Z);
            // note: Java also called AI2Actions.deleteOwner(this) here to remove the egg itself; no C#
            // equivalent delete-owner action exists yet.
        }, hatchDelayMs);
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java spawned a guardian NPC (one of six fixed positions, 282715/282716) on first attack for
        // the small egg (282613), or a random "elite commander" (282712) for the huge egg (282614); the
        // fixed spawn-position table and SpawnEngine's per-instance object spawn aren't ported here.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java buffed its tracked guardian with a "wrath" skill (20176) via SkillEngine on death;
        // skill casting isn't wired at the script layer yet.
    }
}
