using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client requests the abyss player ranking for a given race. Opcode 0x19E.
/// wireRaceId: 0=Elyos, 1=Asmodians. Sends up to 100 entries sorted by AP.
/// </summary>
public sealed class CM_ABYSS_RANKING_PLAYERS : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IPlayerDao         _playerDao;

    private int _wireRaceId;

    public CM_ABYSS_RANKING_PLAYERS(GsClientConnection conn, IPlayerDao playerDao)
    {
        _conn      = conn;
        _playerDao = playerDao;
    }

    public override void Read(ref PacketReader r) => _wireRaceId = r.ReadC();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_conn.ActivePlayer is null) return;

        // wireRaceId 0 = ELYOS (Java convention), 1 = ASMODIANS
        Race race = _wireRaceId == 0 ? Race.ELYOS : Race.ASMODIANS;

        var entries = await _playerDao.GetTopAbyssRankAsync(race, limit: 100, ct);
        await _conn.SendAsync(new SM_ABYSS_RANKING_PLAYERS(_wireRaceId, entries), ct);
    }
}
