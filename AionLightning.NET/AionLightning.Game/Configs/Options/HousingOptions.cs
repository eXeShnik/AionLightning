namespace AionLightning.Game.Configs.Options;

/// <summary>
/// P1 housing subsystem gate. When false (default), house data/ownership maps/owner-flags are still
/// loaded and computed via HousingService, but no SM_HOUSE_OWNER_INFO/SM_HOUSE_ACQUIRE packet is ever
/// sent to a client — the enter-world/login flow is byte-verified against a real client, and
/// broadcasting an unverified 4.5-era housing opcode there could wedge it. Flip to true only after the
/// opcodes are confirmed against a live 4.6 client capture (see the per-packet TODO comments). Mirrors
/// SiegeOptions.Enable's gating rationale/pattern exactly.
/// </summary>
public sealed record HousingOptions
{
    public bool Enable { get; init; } = false;
}
