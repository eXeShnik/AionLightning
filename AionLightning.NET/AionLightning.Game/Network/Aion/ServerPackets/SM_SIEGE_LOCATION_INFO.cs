using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Siege;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Full or partial siege-location ownership broadcast (Java network.aion.serverpackets.SM_SIEGE_LOCATION_INFO).
/// Opcode 0xD1 (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_SIEGE_LOCATION_INFO : AionServerPacket
{
    private readonly byte _infoType; // 0 = reset (every location), 1 = update (single location)
    private readonly IReadOnlyCollection<SiegeLocation> _locations;
    private readonly Player? _player;
    private readonly SiegeService _siegeService;

    public SM_SIEGE_LOCATION_INFO(SiegeService siegeService, Player? player) : base(0xD1)
    {
        _infoType = 0;
        _locations = siegeService.Locations.Values.ToList();
        _player = player;
        _siegeService = siegeService;
    }

    public SM_SIEGE_LOCATION_INFO(SiegeLocation location, SiegeService siegeService, Player? player) : base(0xD1)
    {
        _infoType = 1;
        _locations = [location];
        _player = player;
        _siegeService = siegeService;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_infoType);
        w.WriteH((short)_locations.Count);

        foreach (var loc in _locations)
        {
            w.WriteD(loc.LocationId);

            int legionId = loc.LegionId;
            w.WriteD(legionId);

            byte emblemId = 0, r = 0, g = 0, b = 0;
            if (legionId != 0)
            {
                var legion = _siegeService.GetLegion(legionId);
                if (legion is not null)
                {
                    emblemId = legion.EmblemId;
                    r = legion.EmblemR;
                    g = legion.EmblemG;
                    b = legion.EmblemB;
                }
            }
            // note: Java distinguishes DEFAULT vs CUSTOM emblem (writeD(customEmblemData.length) for
            // CUSTOM). This port's Legion model doesn't track a separate custom-emblem byte blob, so
            // emblemId is written in both cases — extend Legion with custom emblem storage in P2 if
            // pixel emblems need to render correctly on siege location banners.
            w.WriteD(emblemId);
            w.WriteC(255);
            w.WriteC(r);
            w.WriteC(g);
            w.WriteC(b);

            w.WriteC((byte)loc.Race);
            w.WriteC(loc.IsVulnerable ? (byte)2 : (byte)0);
            w.WriteC(loc.CanTeleport(_player) ? (byte)1 : (byte)0);
            w.WriteC((byte)loc.GetNextState());

            w.WriteH(0); // unk
            w.WriteH(1);

            int remaining = loc.LocationId is 2111 or 3111
                ? _siegeService.GetRemainingSiegeTimeInSeconds(loc.LocationId)
                : 10000;
            w.WriteD(remaining);
        }
    }
}
