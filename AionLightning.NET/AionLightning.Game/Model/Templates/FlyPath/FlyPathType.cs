namespace AionLightning.Game.Model.Templates.FlyPath;

/// <summary>
/// Port of Java <c>model.flypath.FlyPathType</c> — categorizes windstream glide-path locations
/// (Java <c>templates.windstreams.Location2D.flyPath</c>), not the flight-master <see cref="FlyPathEntry"/>
/// above despite sharing a package name in Java.
/// note: no windstream template DataHolder is ported yet, so this enum has no consumer in this port;
/// kept for parity and for when SM_WINDSTREAM_ANNOUNCE/windstream data is wired up.
/// </summary>
public enum FlyPathType
{
    Geyser = 0,
    OneWay = 1,
    TwoWay = 2,
}
