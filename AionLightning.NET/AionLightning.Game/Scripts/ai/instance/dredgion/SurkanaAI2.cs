// SurkanaAI2 — Java ai/instance/dredgion/SurkanaAI2.java. Surkana: takes only 1 dmg per hit
// (handled by OneDmgPerHitAI2) and pulls the whole room's aggro when attacked.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("surkana")]
public sealed class SurkanaAI2 : OneDmgPerHitAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java broadcast a CREATURE_AGGRO event to every living NPC within 25m (room-wide aggro
        // pull); known-list iteration and cross-NPC aggro events aren't exposed to scripts yet — NPC aggro
        // stays owned by NpcAiService.
    }
}
