namespace AionLightning.Game.Model.Rift;

/// <summary>
/// Java model.rift.RiftLocation — runtime state for one rift_location entry: whether it is
/// currently open and the set of NPCs (master portal, slave portal, and any spawned guards)
/// live for it. Static id/worldId come from <see cref="RiftTemplate"/>.
/// </summary>
public sealed class RiftLocation(RiftTemplate template)
{
    public int Id => template.Id;
    public int WorldId => template.WorldId;

    public bool Opened { get; set; }

    public List<Npc> Spawned { get; } = [];
}
