// UnstableTriroanAI2 — Java ai/instance/theobomosLab/UnstableTriroanAI2.java. Unstable Triroan boss:
// HP-threshold cascade that casts a self-buff at 99% then spawns/walks elemental summon helpers.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("triroan")]
public sealed class UnstableTriroanAI2 : AggressiveNpcAI2
{
    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java tracked ten descending HP thresholds (99..5%); at 99% it cast skill 16699 on itself,
        // and at each other threshold it spawned one or more elemental summon helpers (280975-280978) and
        // started them walking a named route (WalkManager.startWalking + SM_EMOTION broadcast); skill
        // casting and npc-walker routes aren't wired at the script layer yet.
    }
}
