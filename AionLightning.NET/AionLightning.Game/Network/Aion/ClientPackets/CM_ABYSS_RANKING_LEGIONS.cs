using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client requests the abyss legion ranking for a given race. Opcode 0x154.
/// wireRaceId: 0=Elyos, 1=Asmodians. Sends up to 100 entries sorted by contribution points.
/// </summary>
public sealed class CM_ABYSS_RANKING_LEGIONS : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly ILegionDao         _legionDao;

    private int _wireRaceId;

    public CM_ABYSS_RANKING_LEGIONS(GsClientConnection conn, ILegionDao legionDao)
    {
        _conn      = conn;
        _legionDao = legionDao;
    }

    public override void Read(ref PacketReader r) => _wireRaceId = r.ReadC();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_conn.ActivePlayer is null) return;

        Race race = _wireRaceId == 0 ? Race.ELYOS : Race.ASMODIANS;
        var entries = await _legionDao.GetTopLegionRankAsync(race, limit: 100, ct);
        await _conn.SendAsync(new SM_ABYSS_RANKING_LEGIONS(_wireRaceId, entries), ct);
    }
}
