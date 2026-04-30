using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client selects a bonus title (stat-only; no visual broadcast). Opcode 0x18B.</summary>
public sealed class CM_BONUS_TITLE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IPlayerDao         _playerDao;

    private short _bonusTitleId;

    public CM_BONUS_TITLE(GsClientConnection conn, IPlayerDao playerDao)
    {
        _conn      = conn;
        _playerDao = playerDao;
    }

    public override void Read(ref PacketReader r) => _bonusTitleId = (short)r.ReadH();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // 0xFFFF (as signed short = -1) means clear bonus title
        int bonusTitleId = _bonusTitleId == -1 ? -1 : _bonusTitleId;
        player.BonusTitleId = bonusTitleId;

        await _playerDao.UpdateBonusTitleAsync(player.ObjectId, bonusTitleId, ct);
        await _conn.SendAsync(SM_TITLE_INFO.BonusTitle(bonusTitleId), ct);
    }
}
