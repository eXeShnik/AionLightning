// ElementalIronPrisonAI2 — Java ai/instance/argentManor/ElementalIronPrisonAI2.java. Argent Manor
// prison-boss: triggers a shout event when a player gets close, then cycles random elemental
// skills while any player stays in range, resetting via handleBackHome when they leave.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("elemental_iron_prison")]
public sealed class ElementalIronPrisonAI2 : GeneralNpcAI2
{
    public override void OnCreatureMoved(Creature creature)
    {
        base.OnCreatureMoved(creature);
        // note: Java checked if a moving Player came within 25m and, on first trigger, had a nearby
        // NPC (205498) shout twice via NpcShoutsService. MathUtil distance checks and NpcShoutsService
        // aren't exposed to scripts yet. canThink() always returned false (no C# equivalent).
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        // note: Java closed instance door 76 on first attack, started a 30s-repeating random elemental
        // skill task (19312-19315), and a 2s-repeating "still in range" check that called handleBackHome
        // once no living, visible player remained within 40m. WorldMapInstance doors and known-list
        // player scanning aren't exposed to scripts yet.
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        // note: Java re-opened door 76, removed the 4 elemental debuff effects, and cancelled both
        // tasks. EffectController and door control aren't exposed to scripts yet.
    }

    public override void OnDied()
    {
        base.OnDied();
        // note: Java opened doors 76/26 and deleted a linked npc (701000) via NpcActions.delete; also
        // overrode modifyHealValue to scale incoming heals based on instance player count — neither
        // door control nor a heal-modification hook exists on NpcAi2 yet.
    }
}
