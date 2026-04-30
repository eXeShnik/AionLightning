using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Broadcasts item use animation to zone players. Opcode 0xB7.
/// Wire: D(playerObjId)+D(targetObjId)+D(itemObjId)+D(itemId)+D(time)+C(end)+C(0)+C(1)+D(unk)+C(0)
/// end=1 → instant/completed; end=0 → interrupted. time=0 for instant-use consumables.
/// </summary>
public sealed class SM_ITEM_USAGE_ANIMATION : AionServerPacket
{
    private readonly int _playerObjId;
    private readonly int _targetObjId;
    private readonly int _itemObjId;
    private readonly int _itemId;
    private readonly int _time;
    private readonly byte _end;

    public SM_ITEM_USAGE_ANIMATION(int playerObjId, int itemObjId, int itemId,
        int time = 0, byte end = 1) : base(0xB7)
    {
        _playerObjId = playerObjId;
        _targetObjId = playerObjId;
        _itemObjId   = itemObjId;
        _itemId      = itemId;
        _time        = time;
        _end         = end;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjId);
        w.WriteD(_targetObjId);
        w.WriteD(_itemObjId);
        w.WriteD(_itemId);
        w.WriteD(_time);
        w.WriteC(_end);
        w.WriteC(0);
        w.WriteC(1);
        w.WriteD(0);
        w.WriteC(0);
    }
}
