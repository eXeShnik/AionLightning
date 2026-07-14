namespace AionLightning.Game.Configs.Options;

/// <summary>
/// TownService gate for the login-time SM_TOWNS_LIST broadcast. Town level data always loads and
/// <see cref="Services.TownService.GetTownLevel"/> stays callable regardless of this flag (the housing
/// owner-info townLevel field always reflects the real value) — only the extra login packet is gated,
/// to protect the byte-verified PlayerEnterWorldService packet sequence until SM_TOWNS_LIST's opcode is
/// confirmed against a live 4.6 client capture.
/// </summary>
public sealed record TownOptions
{
    public bool SendTownListOnLogin { get; init; } = false;
}
