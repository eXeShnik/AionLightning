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
    /// Dialog-only NPCs (portal/useitem/quest_use_item/chest/book). Inert like NoAction — no aggro,
    /// no wander, no combat ticks or retaliation; CM_DIALOG_SELECT handles these independently of
    /// the AI tick.
    /// </summary>
    Interaction,

    /// <summary>
    /// Post guards (simple_abyssguard/artifact_protector/siege_protector/"guard"): proactively
    /// aggro-scans, fights, and retaliates exactly like Aggressive, but never random-wanders away
    /// from its spawn post — it only moves along an assigned walker route (if any) and returns
    /// straight to <see cref="Npc.HomePosition"/> after combat like every other archetype.
    /// </summary>
    Guard,

    /// <summary>
    /// Static trigger NPC (Java "trap" ai, e.g. <c>TrapNpcAI2</c>): never aggro-scans, wanders, or
    /// fights through the normal tick. Instead it scans for an enemy within its aggro range each
    /// tick and, on first trigger, casts a skill once and despawns — see
    /// <see cref="AionLightning.Game.Services.NpcAiService"/>'s dedicated Trap tick path.
    /// </summary>
    Trap,
}
