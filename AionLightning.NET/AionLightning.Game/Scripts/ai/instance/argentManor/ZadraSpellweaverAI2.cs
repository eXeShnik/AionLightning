// ZadraSpellweaverAI2 — Java ai/instance/argentManor/ZadraSpellweaverAI2.java. Argent Manor final
// boss: one script drives three related npc ids (217240/217241/217242) through a multi-phase
// HP-threshold event chain involving helper "surkana" spawns, a robot-chastisement sub-event, and
// scripted walk sequences between phases.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("zadra_spellweaver")]
public sealed class ZadraSpellweaverAI2 : AggressiveNpcAI2
{
    public override void OnCreatureMoved(Creature creature)
    {
        base.OnCreatureMoved(creature);
        // note: for npc id 217240, Java triggered a one-time shout pair on the first player that moved
        // nearby, then after 12s attacked a linked npc (282266) for lethal damage (or deleted itself if
        // that npc was gone) and spawned the next-phase npc (217242).
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: for npc id 217241, closed instance door 10 on first attack. For npc id 217242, shouted
        // and started a repeating phase task (skill 19717 on target every 45s, plus a follow-up skill
        // 19824 after 25s once below 90% HP). Both ids also ran an HP-percentage threshold check that
        // spawned helper "surkana" npcs (217241, at 80/70/55/40%) or triggered the robot sub-event
        // (217242, at 75%/35% thresholds).
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        // note: npc id 217241 reset its percentage-threshold list and its 3 surkana helper spawns on
        // spawn; npc id 217242 shouted once; the third id (217240, handled implicitly) disabled its own
        // think loop via canThink (no C# equivalent).
    }

    public override void OnMoveArrived()
    {
        base.OnMoveArrived();
        // note: for npc id 217241, once its FOLLOWING-state walk to a surkana helper or door-npc landed,
        // it cast a skill and (for the door-npc target) deleted itself. For npc id 217242, once its
        // WALKING-state route to the robot sub-event landed, it stopped walking, spawned a "chastisement"
        // npc, and had a nearby robot npc (282189) attack it. All of this depends on AggroList, target
        // resolution, WalkManager and instance-scoped npc lookups that aren't exposed to scripts yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: npc id 217241 spawned a reward-door prop and opened doors 10/210; npc id 217242 cancelled
        // its tasks, opened doors 18/11, spawned a reward prop, deleted its robot/wing/chastisement
        // helpers, and shouted.
    }

    public override void OnDespawned()
    {
        base.OnDespawned();
        // note: Java cleared its percentage-threshold list here.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: npc id 217241 reset its threshold cursor, removed 4 tracked skill effects, respawned its
        // 3 surkana helpers, reset its threshold list, re-opened door 10, and deleted any spawned wing
        // helpers; npc id 217242 deleted its chastisement helper. Both cancelled all pending tasks and
        // reset their local phase flags.
    }
}
