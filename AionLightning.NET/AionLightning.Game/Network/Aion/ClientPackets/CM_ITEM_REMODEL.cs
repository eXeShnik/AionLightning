using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client remodels an item's appearance. Stub — opcode 0x138.</summary>
public sealed class CM_ITEM_REMODEL : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // npcId
        r.ReadD(); // keepItemId
        r.ReadD(); // extractItemId
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
