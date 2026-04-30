using AionLightning.Commons.Network;
using AionLightning.Game.Dao;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client updates the note for a blocked player. Opcode 0x171.</summary>
public sealed class CM_BLOCK_SET_REASON : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly ISocialDao         _socialDao;

    private int    _blockedObjectId;
    private string _reason = string.Empty;

    public CM_BLOCK_SET_REASON(GsClientConnection conn, ISocialDao socialDao)
    {
        _conn      = conn;
        _socialDao = socialDao;
    }

    public override void Read(ref PacketReader r)
    {
        _blockedObjectId = r.ReadD();
        _reason          = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;
        await _socialDao.UpdateBlockReasonAsync(player.ObjectId, _blockedObjectId, _reason, ct);
    }
}
