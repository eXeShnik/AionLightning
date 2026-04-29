using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.GameServer.Clientpackets;

public sealed class CM_ACCOUNT_DISCONNECTED : GsClientPacket
{
    private readonly GsConnection _conn;
    private int _accountId;

    public CM_ACCOUNT_DISCONNECTED(GsConnection conn)
    {
        _conn = conn;
    }

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        var gsi = _conn.GameServerInfo;
        if (gsi == null) return ValueTask.CompletedTask;
        var account = gsi.GetAccount(_accountId);
        if (account != null) gsi.RemoveAccount(account);
        return ValueTask.CompletedTask;
    }
}
