using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client sockets a godstone into a weapon. Stub — opcode 0x139.</summary>
public sealed class CM_GODSTONE_SOCKET : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // npcObjectId
        r.ReadD(); // weaponId
        r.ReadD(); // stoneId
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
