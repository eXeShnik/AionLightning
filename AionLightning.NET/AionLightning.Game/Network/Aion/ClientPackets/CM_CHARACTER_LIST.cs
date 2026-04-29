using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_CHARACTER_LIST : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IPlayerDao _playerDao;
    private readonly IPlayerAppearanceDao _appearanceDao;

    private int _playOk2;

    public CM_CHARACTER_LIST(GsClientConnection conn, IPlayerDao playerDao, IPlayerAppearanceDao appearanceDao)
    {
        _conn = conn;
        _playerDao = playerDao;
        _appearanceDao = appearanceDao;
    }

    public override void Read(ref PacketReader r) => _playOk2 = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var players = await _playerDao.FindByAccountIdAsync(_conn.AccountId, ct);
        var characters = new List<(Player, PlayerAppearance)>(players.Count);

        foreach (var p in players)
        {
            var appearance = await _appearanceDao.FindByPlayerIdAsync(p.ObjectId, ct)
                             ?? new PlayerAppearance();
            characters.Add((p, appearance));
        }

        await _conn.SendAsync(new SM_CHARACTER_LIST(_playOk2, characters), ct);
    }
}
