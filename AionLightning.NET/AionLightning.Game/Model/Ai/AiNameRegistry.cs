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
        ["trap"]           = AiArchetype.Interaction,

        // Guard family — Phase 1 treats guards as aggressive-with-leash (they scan/wander/retaliate
        // like Aggressive). A dedicated Guard archetype (post assist radius, no-leash return rules)
        // is Phase 2 per the survey's port order.
        ["simple_abyssguard"]  = AiArchetype.Aggressive,
        ["artifact_protector"] = AiArchetype.Aggressive,
        ["siege_protector"]    = AiArchetype.Aggressive,
        ["guard"]              = AiArchetype.Aggressive,
    };

    /// <summary>
    /// Resolves an ai-name to its archetype. Case-insensitive. Any unregistered name containing
    /// "guard" falls back to Aggressive; anything else unknown falls back to General — the safe
    /// default, since it retaliates when attacked but never proactively aggros.
    /// </summary>
    public static AiArchetype Resolve(string aiName)
    {
        if (string.IsNullOrEmpty(aiName)) return AiArchetype.General;
        if (Names.TryGetValue(aiName, out var archetype)) return archetype;
        return aiName.Contains("guard", StringComparison.OrdinalIgnoreCase) ? AiArchetype.Aggressive : AiArchetype.General;
    }
}
