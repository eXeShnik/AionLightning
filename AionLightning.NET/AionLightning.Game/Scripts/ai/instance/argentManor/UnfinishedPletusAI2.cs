// UnfinishedPletusAI2 — Java ai/instance/argentManor/UnfinishedPletusAI2.java. Argent Manor boss:
// on first attack closes a door and starts a repeating cast-sequence phase; spawns two walker
// helpers once below 75% HP.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("unfinished_pletus")]
public sealed class UnfinishedPletusAI2 : GeneralNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java closed instance door 26 on first attack and started a 60s-repeating phase task
        // (shout + skill 19304, then after 3s shout + skill 19300 + a 30s-delayed follow-up skill 19303).
        // Once HP dropped to <=75% it spawned two walker helpers (282146) on scripted routes. Door
        // control, NpcShoutsService, SkillEngine casting and WalkManager routing aren't exposed to
        // scripts yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java shouted, cancelled both tasks, opened doors 26/158/10, spawned a reward-linked prop
        // (701013), deleted its walker helpers, and deleted itself via AI2Actions.deleteOwner.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        // note: Java cancelled both tasks and deleted its spawned walker helpers.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java cancelled both tasks, re-opened door 26, reset the helper-spawned flag, and deleted
        // its walker helpers.
    }
}
