using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests in-game shop product info. Stub — opcode 0x183.</summary>
public sealed class CM_IN_GAME_SHOP_INFO : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadC(); // actionId
        r.ReadD(); // categoryId
        r.ReadD(); // listInCategory
        r.ReadS(); // senderName
        r.ReadS(); // senderMessage
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
