using AionLightning.Game.Model.GameObjects.Siege;
using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Services.Siege.Assault;

/// <summary>
/// Java services.siegeservice.Assault&lt;siege extends Siege&lt;?&gt;&gt; — shared lifecycle base for the
/// balaur auto-assault state machines (<see cref="FortressAssault"/>/<see cref="ArtifactAssault"/>).
/// Java tracked its two delayed stages (the initial "dredgion" spawn, then the attacker-wave spawn) as
/// separate cancellable <c>Future</c>s; this port collapses them into a single <see cref="CancellationTokenSource"/>
/// since <see cref="FinishAssault"/> always cancels both stages together anyway (functionally equivalent —
/// see <see cref="FortressAssault"/>'s scheduling method for the two-stage delay chain).
/// </summary>
public abstract class Assault
{
    protected readonly SiegeLocation SiegeLocation;
    protected readonly SiegeNpc? Boss;
    protected readonly int LocationId;

    /// <summary>Java Assault.getWorldId() — public because BalaurAssaultService.CalculateFortressAssault
    /// counts active assaults per world id across all currently-tracked <see cref="FortressAssault"/>s.</summary>
    public int WorldId { get; }

    /// <summary>Cancelled by <see cref="FinishAssault"/>; stops whichever delayed stage is currently in flight.</summary>
    protected CancellationTokenSource? AssaultCts;

    protected Assault(Siege siege)
    {
        SiegeLocation = siege.LocationBase;
        Boss = siege.Boss;
        LocationId = siege.SiegeLocationId;
        WorldId = siege.LocationBase.WorldId;
    }

    /// <summary>Java startAssault(int) — begins the (possibly multi-stage) delayed schedule.</summary>
    public void StartAssault(int delaySeconds) => ScheduleAssault(delaySeconds);

    /// <summary>Java finishAssault(boolean) — cancels any still-pending stage, then notifies the subclass
    /// whether the fortress ended up captured by the balaur (only meaningful when the siege's winning race
    /// really is <see cref="SiegeRace.BALAUR"/>).</summary>
    public void FinishAssault(bool captured)
    {
        AssaultCts?.Cancel();
        OnAssaultFinish(captured && SiegeLocation.Race == SiegeRace.BALAUR);
    }

    protected abstract void OnAssaultFinish(bool captured);

    protected abstract void ScheduleAssault(int delaySeconds);
}
