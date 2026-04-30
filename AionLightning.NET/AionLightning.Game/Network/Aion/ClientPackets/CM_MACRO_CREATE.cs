using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client creates or replaces a macro. Opcode 0x14D.</summary>
public sealed class CM_MACRO_CREATE : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IMacroDao _macroDao;

    private int _position;
    private string _xml = "";

    public CM_MACRO_CREATE(GsClientConnection conn, IMacroDao macroDao)
    {
        _conn     = conn;
        _macroDao = macroDao;
    }

    public override void Read(ref PacketReader r)
    {
        _position = r.ReadC();
        _xml      = r.ReadS();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        player.Macros[_position] = _xml;
        await _macroDao.UpsertAsync(player.ObjectId, _position, _xml, ct);
        await _conn.SendAsync(SM_MACRO_RESULT.Created, ct);
    }
}
