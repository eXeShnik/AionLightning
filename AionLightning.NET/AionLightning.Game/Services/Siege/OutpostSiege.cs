using AionLightning.Game.Model;
using AionLightning.Game.Model.Siege;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services.Siege;

/// <summary>Java services.siegeservice.OutpostSiege — the Elysea/Asmodae underpass outpost siege
/// (no legion or AP/GP participation; ownership is purely by top-damage race).</summary>
public sealed class OutpostSiege : Siege<OutpostLocation>
{
    public OutpostSiege(OutpostLocation location, SiegeService service, ILogger log) : base(location, service, log)
    {
    }

    public override bool IsEndless => false;

    public override void AddAbyssPoints(Player player, int abyssPoints)
    {
        // Java: "No need to control AP".
    }

    public override void AddGloryPoints(Player player, int gloryPoints)
    {
        // Java: "No need to control GP".
    }

    protected override Task OnSiegeStartAsync(CancellationToken ct)
    {
        DeSpawnNpcs(SiegeLocationId);
        Location.IsVulnerable = true;
        SpawnNpcs(SiegeLocationId, Location.Race, SiegeModType.SIEGE);
        InitSiegeBoss();

        // note: Java broadcasts SM_SYSTEM_MESSAGE(1400317/1400318) to every online player here — dropped,
        // no simple string-id SM_SYSTEM_MESSAGE overload plumbing exists for the Underpass ids yet (P3+).

        return BroadcastUpdateAsync(Location, ct);
    }

    protected override async Task OnSiegeFinishAsync(CancellationToken ct)
    {
        Location.IsVulnerable = false;

        if (BossKilled)
        {
            await OnCaptureAsync(ct);
        }
        // note: Java's "not captured" SM_SYSTEM_MESSAGE(1400319/1400320) broadcast — dropped, see
        // OnSiegeStartAsync's note.

        await BroadcastUpdateAsync(Location, ct);
    }

    /// <summary>Java onCapture() — outposts have no legion ownership, only race.
    /// note: Java's top-damage-player announcement + SkillEngine.applyEffectDirectly(12119/12120) race
    /// buff on capture — dropped, SkillEngine has no direct-effect-apply entry point in this port yet
    /// (P3+).</summary>
    private async Task OnCaptureAsync(CancellationToken ct)
    {
        var winner = SiegeCounter.GetWinnerRaceCounter();
        await Service.SetOwnerAsync(SiegeLocationId, winner.SiegeRace, 0, ct);
    }
}
