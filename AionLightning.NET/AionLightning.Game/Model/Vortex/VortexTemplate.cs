namespace AionLightning.Game.Model.Vortex;

/// <summary>Java model.templates.vortex.HomePoint/ResurrectionPoint/StartPoint — all three carry the
/// exact same map/x/y/z/h shape in dimensional_vortex.xml, so this port collapses them into one value
/// type instead of three near-identical classes.</summary>
public readonly record struct VortexPoint(int WorldId, float X, float Y, float Z, byte Heading);

/// <summary>
/// Java model.templates.vortex.VortexTemplate — one &lt;vortex_location&gt; entry from
/// data/static_data/vortex/dimensional_vortex.xml. Only two locations ever exist in the 4.6 data set
/// (id 0 = Theobomos invasion of Marchutan's realm, id 1 = Brusthonin invasion of Kaisinel's realm —
/// see <see cref="Services.VortexService"/>'s doc comment), each carrying the defending/offending race
/// pair and the three fixed points the invasion flow uses: <see cref="Home"/> (where a kicked/leaving
/// invader is returned), <see cref="Resurrection"/> (where a dead invader revives while the invasion
/// is active — see Java PlayerReviveService), and <see cref="Start"/> (where the entry portal drops an
/// accepted invader).
/// </summary>
public sealed record VortexTemplate(
    int Id,
    Race DefendsRace,
    Race OffenceRace,
    VortexPoint Home,
    VortexPoint Resurrection,
    VortexPoint Start);
