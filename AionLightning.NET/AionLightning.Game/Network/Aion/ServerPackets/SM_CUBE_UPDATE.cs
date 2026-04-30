using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Updates cube (inventory) and stigma slot sizes. Opcode 0x82.</summary>
public sealed class SM_CUBE_UPDATE : AionServerPacket
{
    private readonly byte _action;
    private readonly byte _actionValue;
    private readonly int  _itemCount;

    /// <summary>action=0: sends cube size and current item count.</summary>
    public static SM_CUBE_UPDATE CubeSize(int itemCount)  => new(0, 0, itemCount);

    /// <summary>action=6: sends advanced stigma slot size.</summary>
    public static SM_CUBE_UPDATE StigmaSlots(byte slots)  => new(6, slots, 0);

    private SM_CUBE_UPDATE(byte action, byte actionValue, int itemCount) : base(0x82)
    {
        _action      = action;
        _actionValue = actionValue;
        _itemCount   = itemCount;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        w.WriteC(_actionValue);
        if (_action == 0)
        {
            w.WriteD(_itemCount);
            w.WriteC(0); // npcExpands
            w.WriteC(0); // questExpands
            w.WriteC(0); // unk
        }
        // action=6 writes nothing beyond the two header bytes
    }
}
