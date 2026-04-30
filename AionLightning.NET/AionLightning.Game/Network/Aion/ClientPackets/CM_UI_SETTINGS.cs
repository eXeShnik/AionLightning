using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client saves UI layout/settings block. Stub — opcode 0xA8.</summary>
public sealed class CM_UI_SETTINGS : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadC(); // settingsType
        r.ReadH(); // unknown
        int size = r.ReadH();
        if (size > 0 && r.Remaining >= size)
            r.ReadB(size); // UI data blob
    }

    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
