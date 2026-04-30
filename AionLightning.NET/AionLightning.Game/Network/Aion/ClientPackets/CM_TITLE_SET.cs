using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client equips or unequips an active title. Opcode 0x129.</summary>
public sealed class CM_TITLE_SET : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IPlayerDao         _playerDao;

    private short _titleId;

    public CM_TITLE_SET(GsClientConnection conn, IPlayerDao playerDao)
    {
        _conn      = conn;
        _playerDao = playerDao;
    }

    public override void Read(ref PacketReader r) => _titleId = (short)r.ReadH();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // 0xFFFF (as signed short = -1) means unequip
        int titleId = _titleId == -1 ? -1 : _titleId;
        player.TitleId = titleId;

        await _playerDao.UpdateTitleAsync(player.ObjectId, titleId, ct);
        await _conn.SendAsync(SM_TITLE_INFO.ActiveTitle(titleId), ct);
    }
}
