namespace AionLightning.Game.Model.Instance;

/// <summary>
/// Instance scoreboard phase (Java <c>model.instance.InstanceScoreType</c>), written into
/// <c>SM_INSTANCE_SCORE</c> so the client renders the right scoreboard state (waiting room, running
/// timer, final results).
/// </summary>
public enum InstanceScoreType
{
    PREPARING     = 1 * 1024 * 1024,
    START_PROGRESS = 2 * 1024 * 1024,
    END_PROGRESS   = 3 * 1024 * 1024,
}

public static class InstanceScoreTypeExtensions
{
    public static bool IsPreparing(this InstanceScoreType type)     => type == InstanceScoreType.PREPARING;
    public static bool IsStartProgress(this InstanceScoreType type) => type == InstanceScoreType.START_PROGRESS;
    public static bool IsEndProgress(this InstanceScoreType type)   => type == InstanceScoreType.END_PROGRESS;
}
