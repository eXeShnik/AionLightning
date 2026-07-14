using AionLightning.Game.Model.Templates.Siege;

namespace AionLightning.Game.Model.Siege;

/// <summary>Java model.siege.ArtifactLocation. The Java isStandAlone()/getOwningFortress() methods
/// reached into the SiegeService singleton directly — moved onto SiegeService itself in this port
/// (SiegeService.IsStandaloneArtifact/GetOwningFortress) to keep the model free of a service
/// dependency, per this port's DI-over-statics convention.</summary>
public sealed class ArtifactLocation : SiegeLocation
{
    public ArtifactStatus Status { get; set; } = ArtifactStatus.IDLE;

    public ArtifactLocation(SiegeLocationTemplate template) : base(template)
    {
        IsVulnerable = true; // artifacts are always vulnerable
    }

    public override int GetNextState() => StateVulnerable;

    /// <summary>Java getCoolDown() — remaining cooldown in seconds, 0 once elapsed.</summary>
    public int GetCoolDownSeconds()
    {
        long cdMillis = Template.Activation?.CdMillis ?? 0;
        long elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - LastArtifactActivation;
        return elapsed > cdMillis ? 0 : (int)((cdMillis - elapsed) / 1000);
    }
}
