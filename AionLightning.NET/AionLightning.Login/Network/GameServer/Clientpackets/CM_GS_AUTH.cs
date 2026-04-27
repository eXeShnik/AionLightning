using AionLightning.Commons.Network;
using AionLightning.Login.Network.GameServer;

namespace AionLightning.Login.Network.GameServer.Clientpackets;

public sealed class CM_GS_AUTH : GsClientPacket
{
    private byte _gameServerId;
    private string _password = string.Empty;
    private int _maxPlayers;
    private int _port;

    public override void Read(ref PacketReader r)
    {
        _gameServerId = r.ReadC();
        _password = r.ReadS();
        _maxPlayers = r.ReadD();
        _port = r.ReadH();
        // TODO M2: read ip ranges and default address
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        // TODO M2: GameServerTable.RegisterGameServer, send SM_GS_AUTH_RESPONSE
        return ValueTask.CompletedTask;
    }
}
