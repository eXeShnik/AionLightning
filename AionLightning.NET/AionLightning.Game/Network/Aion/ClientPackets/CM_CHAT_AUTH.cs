using AionLightning.Commons.Network;
using AionLightning.Game.Network.Cs;
using AionLightning.Game.Network.Cs.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_CHAT_AUTH : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly CsConnectionHolder _csHolder;

    public CM_CHAT_AUTH(GsClientConnection conn, CsConnectionHolder csHolder)
    {
        _conn = conn;
        _csHolder = csHolder;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // objectId (unused)
        r.ReadB(6); // MAC address (unused)
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var csConn = _csHolder.Current;
        if (csConn is null) return;

        // playerLogin is account name; nick is player name
        await csConn.SendAsync(
            new SM_CS_PLAYER_AUTH(player.ObjectId, player.Name, player.Name), ct);
    }
}
