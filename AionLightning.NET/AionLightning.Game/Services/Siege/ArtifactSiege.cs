using AionLightning.Game.Model;
using AionLightning.Game.Model.Siege;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services.Siege;

/// <summary>Java services.siegeservice.ArtifactSiege — a standalone (non-fortress-owned) artifact,
/// perpetually re-sieged (<see cref="IsEndless"/>): as soon as one cycle finishes it restarts itself.</summary>
public sealed class ArtifactSiege : Siege<ArtifactLocation>
{
    public ArtifactSiege(ArtifactLocation location, SiegeService service, ILogger log) : base(location, service, log)
    {
    }

    public override bool IsEndless => true;

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
        InitSiegeBoss();
        return Task.CompletedTask;
    }

    protected override async Task OnSiegeFinishAsync(CancellationToken ct)
    {
        DeSpawnNpcs(SiegeLocationId);

        if (BossKilled)
            await OnCaptureAsync(ct);
        else
            Log.LogError("Artifact siege (artifactId:{Id}) ended without killing a boss.", SiegeLocationId);

        SpawnNpcs(SiegeLocationId, Location.Race, SiegeModType.PEACE);
        await BroadcastUpdateAsync(Location, ct);

        // Java: artifact sieges are endless — the cycle immediately restarts on the same location.
        await Service.StartSiegeAsync(SiegeLocationId, ct);
    }

    /// <summary>Java onCapture().
    /// note: Java's SM_SYSTEM_MESSAGE(1320002/1320004) capture announcement — dropped, needs the
    /// DescriptionId-based SM_SYSTEM_MESSAGE overload not ported yet (P3+).</summary>
    private async Task OnCaptureAsync(CancellationToken ct)
    {
        var winner = SiegeCounter.GetWinnerRaceCounter();
        int legionId = winner.SiegeRace == SiegeRace.BALAUR ? 0 : winner.WinnerLegionId ?? 0;
        await Service.SetOwnerAsync(SiegeLocationId, winner.SiegeRace, legionId, ct);
    }
}
