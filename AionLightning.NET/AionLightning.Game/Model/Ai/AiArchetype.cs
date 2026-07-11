namespace AionLightning.Game.Model.Ai;

/// <summary>
/// Behavioral archetype for an NPC, resolved from its ai-name template attribute (see
/// <see cref="AiNameRegistry"/>). NpcAiService remains the tick driver and combat executor for
/// every archetype — the archetype only gates WHETHER an NPC proactively aggro-scans,
/// wanders/patrols, or retaliates when attacked; it never changes combat math (M194-M380).
/// </summary>
public enum AiArchetype
{
    /// <summary>Proactively aggro-scans for players, wanders/patrols when idle, retaliates when attacked.</summary>
    Aggressive,

    /// <summary>No proactive aggro-scan; wanders/patrols when idle; retaliates when attacked (Java "general" ai).</summary>
    General,

    /// <summary>Fully inert: no aggro, no wander/patrol, no combat ticks or retaliation (Java "noaction"/"dummy" ai).</summary>
    NoAction,

    /// <summary>
    /// Dialog-only NPCs (portal/useitem/quest_use_item/chest/book/trap). Inert like NoAction in
    /// Phase 1 — kept distinct so a dedicated interaction layer can be added later without another
    /// registry pass.
    /// </summary>
    Interaction,
}
