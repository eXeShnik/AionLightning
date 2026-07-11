namespace AionLightning.Game.Model.Ai;

/// <summary>
/// Maps an NPC template's ai-name string (npc_templates.xml "ai" attribute) to its behavioral
/// <see cref="AiArchetype"/>. Seeded from the C3 survey (migration_plan.md "C3 survey
/// (2026-07-11)") instead of porting Java's ~457 per-content AI script classes — names not
/// listed here fall back to the nearest archetype rather than requiring a dedicated port.
/// </summary>
public static class AiNameRegistry
{
    private static readonly Dictionary<string, AiArchetype> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aggressive"]     = AiArchetype.Aggressive,
        ["general"]        = AiArchetype.General,
        ["noaction"]       = AiArchetype.NoAction,
        ["dummy"]          = AiArchetype.NoAction,
        ["portal"]         = AiArchetype.Interaction,
        ["useitem"]        = AiArchetype.Interaction,
        ["quest_use_item"] = AiArchetype.Interaction,
        ["chest"]          = AiArchetype.Interaction,
        ["book"]           = AiArchetype.Interaction,

        // C3 Phase 2: traps fire a skill once when an enemy enters range, then despawn — never
        // reward XP/loot (Java TrapNpcAI2.pollInstance: SHOULD_REWARD/SHOULD_DECAY/SHOULD_RESPAWN
        // all NEGATIVE). See NpcAiService's dedicated Trap tick path.
        ["trap"]           = AiArchetype.Trap,

        // Guard family — proactively aggro-scans/fights/retaliates like Aggressive, but never
        // random-wanders away from its post (NpcAiService gates canWander=false for Guard).
        ["simple_abyssguard"]  = AiArchetype.Guard,
        ["artifact_protector"] = AiArchetype.Guard,
        ["siege_protector"]    = AiArchetype.Guard,
        ["guard"]              = AiArchetype.Guard,
    };

    /// <summary>
    /// Resolves an ai-name to its archetype. Case-insensitive. Any unregistered name containing
    /// "guard" falls back to Guard; anything else unknown falls back to General — the safe
    /// default, since it retaliates when attacked but never proactively aggros.
    /// </summary>
    public static AiArchetype Resolve(string aiName)
    {
        if (string.IsNullOrEmpty(aiName)) return AiArchetype.General;
        if (Names.TryGetValue(aiName, out var archetype)) return archetype;
        return aiName.Contains("guard", StringComparison.OrdinalIgnoreCase) ? AiArchetype.Guard : AiArchetype.General;
    }

    /// <summary>
    /// Java AIQuestion.SHOULD_REWARD (see NpcController.doReward, gated by the same poll): only
    /// Aggressive/General/Guard NPCs grant XP/loot/AP on death. NoAction, Interaction, and Trap
    /// NPCs never do — matches TrapNpcAI2.pollInstance's explicit SHOULD_REWARD == NEGATIVE and the
    /// non-combat nature of dialog/dummy NPCs.
    /// </summary>
    public static bool ShouldReward(string aiName)
        => Resolve(aiName) is AiArchetype.Aggressive or AiArchetype.General or AiArchetype.Guard;
}
