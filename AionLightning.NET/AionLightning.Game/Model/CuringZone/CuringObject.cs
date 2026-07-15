using AionLightning.Game.Model.Templates.CuringZones;

namespace AionLightning.Game.Model.CuringZone;

/// <summary>Java model.curingzone.CuringObject — a point+range curing spot (NOT a zone polygon). Java
/// gave it its own <c>NpcKnownList</c> to enumerate nearby players; here <see cref="World.World.GetPlayersInScope"/>
/// (same WorldId+InstanceId) plus a plain distance check against <see cref="Range"/> serves the same purpose,
/// since this object is never itself targeted/attacked and needs no knownlist bookkeeping of its own.</summary>
public sealed class CuringObject : VisibleObject
{
    public CuringTemplate Template { get; }
    public float Range { get; }

    public CuringObject(CuringTemplate template, int instanceId = 0)
    {
        Template = template;
        Range    = template.Range;
        Position = new Position(template.X, template.Y, template.Z, 0, template.MapId, instanceId);
        Name     = string.Empty;
    }
}
