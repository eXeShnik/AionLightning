using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client updates appearance/cosmetic settings. Stub — opcode 0x167.</summary>
public sealed class CM_APPEARANCE : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        byte type = (byte)r.ReadC();
        r.ReadC(); // unk
        r.ReadH(); // unk
        r.ReadD(); // itemObjId
        if (type == 0 || type == 1)
            r.ReadS(); // name (char or legion rename)
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
