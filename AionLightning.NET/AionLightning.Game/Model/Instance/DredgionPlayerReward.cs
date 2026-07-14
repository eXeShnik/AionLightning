namespace AionLightning.Game.Model.Instance;

/// <summary>Per-player Dredgion score row (Java <c>model.instance.playerreward.DredgionPlayerReward</c>).</summary>
public sealed class DredgionPlayerReward : InstancePlayerReward
{
    public int ZoneCaptured { get; private set; }

    public DredgionPlayerReward(int ownerObjectId) : base(ownerObjectId) { }

    public void CaptureZone() => ZoneCaptured++;
}
