using AionLightning.Commons.Network;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_INFLUENCE_RATIO.
/// Opcode 0x55 (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_INFLUENCE_RATIO : AionServerPacket
{
    private readonly SiegeService _siegeService;

    public SM_INFLUENCE_RATIO(SiegeService siegeService) : base(0x55)
    {
        _siegeService = siegeService;
    }

    public override void Write(ref PacketWriter w)
    {
        var inf = _siegeService.GetInfluence();

        w.WriteD(_siegeService.GetSecondsBeforeHourEnd());
        w.WriteF(inf.GlobalElyos);
        w.WriteF(inf.GlobalAsmodians);
        w.WriteF(inf.GlobalBalaur);
        w.WriteH(4); // maps count
        w.WriteD(210050000);
        w.WriteF(inf.InggisonElyos);
        w.WriteF(inf.InggisonAsmodians);
        w.WriteF(inf.InggisonBalaur);
        w.WriteD(220070000);
        w.WriteF(inf.GelkmarosElyos);
        w.WriteF(inf.GelkmarosAsmodians);
        w.WriteF(inf.GelkmarosBalaur);
        w.WriteD(400010000);
        w.WriteF(inf.AbyssElyos);
        w.WriteF(inf.AbyssAsmodians);
        w.WriteF(inf.AbyssBalaur);
        w.WriteD(600030000);
        w.WriteF(inf.TiamarantaElyos);
        w.WriteF(inf.TiamarantaAsmodians);
        w.WriteF(inf.TiamarantaBalaur);
    }
}
