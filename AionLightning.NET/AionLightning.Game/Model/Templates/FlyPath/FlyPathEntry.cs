namespace AionLightning.Game.Model.Templates.FlyPath;

/// <summary>
/// A flight-master glide route loaded from <c>flypath_template.xml</c> (Java
/// <c>model.templates.flypath.FlyPathEntry</c>). <see cref="Id"/> matches the <c>loc_id</c> of the
/// FLIGHT-type &lt;telelocation&gt; entry in <c>npc_teleporter.xml</c> that offers this destination.
/// <see cref="TimeMs"/> is Java's <c>time</c> attribute (seconds) converted to milliseconds once at
/// load time (Java <c>getTimeInMs()</c>), since the arrival-side anti-bug check compares against
/// elapsed wall-clock milliseconds.
/// </summary>
public sealed record FlyPathEntry(
    short Id,
    float StartX, float StartY, float StartZ, int StartWorldId,
    float EndX, float EndY, float EndZ, int EndWorldId,
    int TimeMs);
