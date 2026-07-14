namespace AionLightning.Game.Configs.Options;

/// <summary>
/// AutoGroupService (PvP/co-op instance matchmaking) gate. Mirrors SiegeOptions/HousingOptions'
/// rationale: when false (default), CM_AUTO_GROUP is rejected before it ever reaches
/// <see cref="Services.AutoGroupService"/> — no queue accumulates, no instance channel gets reserved,
/// and no SM_AUTO_GROUP is ever broadcast to a client. AutoGroupService itself also checks this flag
/// (defense in depth, same pattern as SiegeService), so a future caller that bypasses the packet
/// handler still gets a no-op. Flip to true only after the auto-formed-team teleport flow has been
/// verified end-to-end against a live 4.6 client capture.
/// </summary>
public sealed record AutoGroupOptions
{
    public bool Enable { get; init; } = false;
}
