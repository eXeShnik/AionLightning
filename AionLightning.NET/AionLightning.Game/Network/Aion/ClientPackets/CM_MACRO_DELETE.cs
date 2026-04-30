using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client deletes a macro by position. Opcode 0x172.</summary>
public sealed class CM_MACRO_DELETE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IMacroDao _macroDao;

    private int _position;

    public CM_MACRO_DELETE(GsClientConnection conn, IMacroDao macroDao)
    {
        _conn     = conn;
        _macroDao = macroDao;
    }

    public override void Read(ref PacketReader r) => _position = r.ReadC();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        player.Macros.Remove(_position);
        await _macroDao.DeleteAsync(player.ObjectId, _position, ct);
        await _conn.SendAsync(SM_MACRO_RESULT.Deleted, ct);
    }
}
