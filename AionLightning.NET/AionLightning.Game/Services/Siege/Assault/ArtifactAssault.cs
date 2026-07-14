namespace AionLightning.Game.Services.Siege.Assault;

/// <summary>
/// Java services.siegeservice.ArtifactAssault — both lifecycle hooks are empty stubs upstream too
/// (Java's own TODO list: "Send Peace Dredgion without assault" / "Artifact Siege"). Kept as a no-op
/// pass-through so <see cref="BalaurAssaultService"/>'s switch over siege type compiles/dispatches
/// symmetrically with <see cref="FortressAssault"/>; in practice it's never reachable today since
/// <c>BalaurAssaultService.CalculateArtifactAssault</c> always returns false (also a direct port of
/// Java's own unimplemented TODO).
/// </summary>
public sealed class ArtifactAssault : Assault
{
    public ArtifactAssault(ArtifactSiege siege) : base(siege)
    {
    }

    protected override void ScheduleAssault(int delaySeconds)
    {
    }

    protected override void OnAssaultFinish(bool captured)
    {
    }
}
