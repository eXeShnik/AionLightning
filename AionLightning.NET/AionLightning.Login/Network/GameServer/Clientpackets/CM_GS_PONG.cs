using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.GameServer.Clientpackets;

public sealed class CM_GS_PONG : GsClientPacket
{
    private byte _serverId;
    private int _pid;

    public override void Read(ref PacketReader r)
    {
        _serverId = r.ReadC();
        _pid = r.ReadD();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        GameServerTable.Pong(_serverId, _pid);
        return ValueTask.CompletedTask;
    }
}
