using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client blocks a player by name. Opcode 0x144.</summary>
public sealed class CM_BLOCK_ADD : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IPlayerDao         _playerDao;
    private readonly ISocialDao         _socialDao;

    private string _targetName = string.Empty;

    public CM_BLOCK_ADD(GsClientConnection conn, IPlayerDao playerDao, ISocialDao socialDao)
    {
        _conn      = conn;
        _playerDao = playerDao;
        _socialDao = socialDao;
    }

    public override void Read(ref PacketReader r) => _targetName = r.ReadS();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var target = await _playerDao.FindByNameAsync(_targetName, ct);
        if (target is null) return;

        await _socialDao.AddBlockAsync(player.ObjectId, target.ObjectId, "", ct);

        var blocks = await _socialDao.GetBlocksAsync(player.ObjectId, ct);
        await _conn.SendAsync(new SM_BLOCK_LIST(blocks), ct);
    }
}
