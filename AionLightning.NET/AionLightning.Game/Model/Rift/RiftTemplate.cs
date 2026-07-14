namespace AionLightning.Game.Model.Rift;

/// <summary>
/// Java model.templates.rift.RiftTemplate — one &lt;rift_location&gt; entry from
/// data/static_data/rift/rift_locations.xml: the location id (matches the id used by
/// <see cref="RiftEnum"/> and the rift_schedule.xml cron table) and the world it opens in.
/// </summary>
public sealed record RiftTemplate(int Id, int WorldId);
