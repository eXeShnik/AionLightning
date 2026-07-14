// KuharaTheVolatileAI2 — Java ai/instance/rentusBase/KuharaTheVolatileAI2.java. Rentus Base boss:
// alternates an "active" cast-sequence phase with a "bombs" phase that charges barrel adds toward it,
// gated by instance doors.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kuhara_the_volatile")]
public sealed class KuharaTheVolatileAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (!_isHome) return;
        _isHome = false;
        SendMsg(1500393);
        // note: Java opened instance door 43 and closed door 150 here, then started a barrel-spawn loop
        // (random point clusters of npc 282394) that itself triggered a 14s "active" cast sequence
        // (skills 19703-19705) followed by an 11s bomb-npc (282396) charge-to-boss phase, re-targeting the
        // most-hated attacker afterwards — instance doors, random-point spawning, move-to-target, and
        // AggroList have no C# equivalent at the script layer.
    }

    public override void OnDespawned()
    {
        CancelTasks();
        base.OnDespawned();
    }

    public override void OnBackHome()
    {
        _isHome = true;
        CancelTasks();
        base.OnBackHome();
        // note: Java also reset the active/bomb phase and toggled instance doors 43/150 back — see the
        // OnAttack note above.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java deleted the barrel/bomb npcs (282394/282396), spawned the reward chest (219215), and
        // reset instance doors 43/150 — WorldMapInstance access and NPC delete aren't exposed to scripts
        // yet.
    }
}
