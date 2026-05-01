using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Notifies all group members of a loot roll/bid event. Opcode 0x87.
/// playerId=0 means "open the roll window"; playerId=winnerId means "this player won".
/// luck carries the rolled value (1–100) or bid amount; 0xFFFFFFFF signals all-passed.
/// </summary>
public sealed class SM_GROUP_LOOT : AionServerPacket
{
    private readonly int  _groupId;
    private readonly int  _index;
    private readonly int  _itemId;
    private readonly int  _npcObjectId;
    private readonly byte _distributionId;
    private readonly int  _playerId;
    private readonly int  _luck;

    public SM_GROUP_LOOT(int groupId, int playerId, int itemId, int npcObjectId,
        byte distributionId, int luck, int index) : base(0x87)
    {
        _groupId        = groupId;
        _playerId       = playerId;
        _itemId         = itemId;
        _npcObjectId    = npcObjectId;
        _distributionId = distributionId;
        _luck           = luck;
        _index          = index;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_groupId);
        w.WriteD(_index);
        w.WriteD(1);             // unk2 = 1
        w.WriteD(_itemId);
        w.WriteC(0);             // unk3 (3.0)
        w.WriteC(0);             // (3.5)
        w.WriteC(0);             // (4.6)
        w.WriteC(0);             // (extra)
        w.WriteD(_npcObjectId);
        w.WriteC(_distributionId);
        w.WriteD(_playerId);
        w.WriteD(_luck);
    }
}
