using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client submits character passkey. Stub — opcode 0x190.</summary>
public sealed class CM_CHARACTER_PASSKEY : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        int type = r.ReadH(); // 0=new, 2=update, 3=input
        r.ReadB(32);           // passkey (UTF-16LE fixed block)
        if (type == 2)
            r.ReadB(32);       // newPasskey
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
