namespace AionLightning.Game.Model.Siege;

/// <summary>Java model.siege.ArtifactStatus. Values match the Java id ordinals exactly — the raw
/// value is what SM_ABYSS_ARTIFACT_INFO writes on the wire.</summary>
public enum ArtifactStatus
{
    IDLE = 0,
    ACTIVATION = 1,
    CASTING = 2,
    ACTIVATED = 3,
}
