using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client replaces an inventory item. Stub — opcode 0x170.</summary>
public sealed class CM_REPLACE_ITEM : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadC(); // sourceStorageType (signed byte in Java, same width)
        r.ReadD(); // sourceItemObjId
        r.ReadC(); // replaceStorageType
        r.ReadD(); // replaceItemObjId
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
