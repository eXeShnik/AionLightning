using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Ls.ClientPackets;

public sealed class CM_MACBAN_LIST : LsClientPacket
{
    private int _count;

    public override void Read(ref PacketReader r)
    {
        _count = r.ReadD();
        for (int i = 0; i < _count; i++)
        {
            _ = r.ReadS();  // mac
            _ = r.ReadQ();  // time
            _ = r.ReadS();  // details
        }
    }

    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
