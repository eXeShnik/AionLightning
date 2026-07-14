namespace AionLightning.Game.Configs.Options;

/// <summary>
/// Static-door subsystem gate. When false (default), door templates still load and doors still spawn
/// into the per-channel registry, so DoorService's state + the GeneralInstanceHandler/NpcAi2 script
/// helpers all work — but no SM_EMOTION door-state packet is ever broadcast to a client. Doors are a
/// brand-new subsystem carrying a client packet (CM_OPEN_STATICDOOR, 0xF5) that had never been wired to
/// real door state before this port, and no live-client byte capture has verified the door-open flow
/// end-to-end yet. Flip to true only after that verification. Mirrors HousingOptions.Enable/
/// SiegeOptions.Enable's gating rationale exactly.
/// note: unlike Housing/Siege, nothing sends door state at enter-world/login today (Java's
/// StaticObjectController has no onSee hook — doors are static client geometry, not a spawned visible
/// object like an Npc), so this gate only protects the CM_OPEN_STATICDOOR / script-driven open-close path.
/// </summary>
public sealed record DoorOptions
{
    public bool Enable { get; init; } = false;
}
