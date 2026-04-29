using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_L2AUTH_LOGIN_CHECK : AionClientPacket
{
    public int PlayOk2   { get; private set; }
    public int PlayOk1   { get; private set; }
    public int AccountId { get; private set; }
    public int LoginOk   { get; private set; }

    public override void Read(ref PacketReader r)
    {
        PlayOk2   = r.ReadD();
        PlayOk1   = r.ReadD();
        AccountId = r.ReadD();
        LoginOk   = r.ReadD();
    }

    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
