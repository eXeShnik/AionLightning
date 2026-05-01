using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Updates cube (inventory) and stigma slot sizes. Opcode 0x82.</summary>
public sealed class SM_CUBE_UPDATE : AionServerPacket
{
    private readonly byte _action;
    private readonly byte _actionValue;
    private readonly int  _itemCount;
    private readonly byte _npcExpands;
    private readonly byte _questExpands;

    /// <summary>action=0: sends bag item count and cube expand levels.</summary>
    public static SM_CUBE_UPDATE CubeSize(int itemCount, int npcExpands = 0, int questExpands = 0)
        => new(0, 0, itemCount, (byte)npcExpands, (byte)questExpands);

    /// <summary>action=6: sends advanced stigma slot size.</summary>
    public static SM_CUBE_UPDATE StigmaSlots(byte slots)  => new(6, slots, 0, 0, 0);

    private SM_CUBE_UPDATE(byte action, byte actionValue, int itemCount, byte npcExpands, byte questExpands) : base(0x82)
    {
        _action       = action;
        _actionValue  = actionValue;
        _itemCount    = itemCount;
        _npcExpands   = npcExpands;
        _questExpands = questExpands;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        w.WriteC(_actionValue);
        if (_action == 0)
        {
            w.WriteD(_itemCount);
            w.WriteC(_npcExpands);   // npcExpands
            w.WriteC(_questExpands); // questExpands
            w.WriteC(0); // unk
        }
        // action=6 writes nothing beyond the two header bytes
    }
}
