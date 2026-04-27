using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ClientPackets;

public sealed class CM_LOGIN : AionClientPacket
{
    private byte[] _data = Array.Empty<byte>();

    public override void Read(ref PacketReader r)
    {
        r.Skip(4);
        _data = r.ReadB(128);
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        // TODO M2: RSA-decrypt _data, extract user/password, call AccountController.Login
        return ValueTask.CompletedTask;
    }
}
