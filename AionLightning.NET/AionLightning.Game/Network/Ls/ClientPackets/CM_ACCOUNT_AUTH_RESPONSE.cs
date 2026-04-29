using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Ls.ClientPackets;

public sealed class CM_ACCOUNT_AUTH_RESPONSE : LsClientPacket
{
    private readonly LsConnection _conn;
    private readonly GameAccountRegistry _registry;
    private int _accountId;
    private bool _result;

    public CM_ACCOUNT_AUTH_RESPONSE(LsConnection conn, GameAccountRegistry registry)
    {
        _conn = conn;
        _registry = registry;
    }

    public override void Read(ref PacketReader r)
    {
        _accountId = r.ReadD();
        _result = r.ReadC() == 1;

        if (_result)
        {
            _ = r.ReadS();   // accountName
            _ = r.ReadQ();   // accumulatedOnlineTime
            _ = r.ReadQ();   // accumulatedRestTime
            _ = r.ReadC();   // accessLevel
            _ = r.ReadC();   // membership
            _ = r.ReadQ();   // toll
        }
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        _registry.CompleteAuth(_accountId, _result);
        return ValueTask.CompletedTask;
    }
}
