namespace AionLightning.Game.Configs.Options;

/// <summary>
/// P1 siege subsystem gate. When false (default), siege data/persistence/getters are still loaded
/// and exposed via SiegeService, but no SM_SIEGE_*/SM_FORTRESS_*/SM_INFLUENCE_RATIO/
/// SM_ABYSS_ARTIFACT_INFO/SM_SHIELD_EFFECT packet is ever sent to a client — the enter-world/login
/// flow is byte-verified against a real client, and broadcasting an unverified 4.5-era siege opcode
/// there could wedge it. Flip to true only after the opcodes are confirmed against a live 4.6 client
/// capture (see the per-packet TODO comments).
/// </summary>
public sealed record SiegeOptions
{
    public bool Enable { get; init; } = false;
}
