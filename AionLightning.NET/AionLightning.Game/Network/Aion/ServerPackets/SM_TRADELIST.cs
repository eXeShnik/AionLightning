using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends the NPC shop goods-list IDs to the client. The client looks up item details
/// from its own data files using those IDs. Opcode 0x8F.
/// </summary>
public sealed class SM_TRADELIST : AionServerPacket
{
    private readonly int _npcObjectId;
    private readonly IReadOnlyList<int> _goodsListIds;
    private readonly int _sellPriceRate; // 100 = full base price when player sells
    private readonly int _buyPriceRate;  // 100 = full base price when player buys

    public SM_TRADELIST(int npcObjectId, IReadOnlyList<int> goodsListIds,
        int sellPriceRate = 100, int buyPriceRate = 100)
        : base(0x8F)
    {
        _npcObjectId   = npcObjectId;
        _goodsListIds  = goodsListIds;
        _sellPriceRate = sellPriceRate;
        _buyPriceRate  = buyPriceRate;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_npcObjectId);
        w.WriteC(0);                          // trade NPC type: 0 = NORMAL
        w.WriteD(_sellPriceRate);
        w.WriteD(_buyPriceRate);
        w.WriteC(1);                          // 4.6 flag
        w.WriteC(1);                          // 4.6 flag
        w.WriteH((short)_goodsListIds.Count); // tab count
        foreach (var id in _goodsListIds)
            w.WriteD(id);
        w.WriteH(0);                          // limited items count
    }
}
