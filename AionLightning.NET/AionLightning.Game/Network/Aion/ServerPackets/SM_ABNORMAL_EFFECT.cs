using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends abnormal effect (buff/debuff) state for a creature. Opcode 0x32.</summary>
public sealed class SM_ABNORMAL_EFFECT : AionServerPacket
{
    private readonly int _effectedId;
    private readonly byte _effectType; // 1 = creature, 2 = player

    public SM_ABNORMAL_EFFECT(int effectedId, bool isPlayer) : base(0x32)
    {
        _effectedId = effectedId;
        _effectType = isPlayer ? (byte)2 : (byte)1;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_effectedId);
        w.WriteC(_effectType);
        w.WriteD(0); // time
        w.WriteD(0); // abnormals mask
        w.WriteD(0); // unk
        w.WriteC(0x7F); // slots (127 = max)
        w.WriteH(0);    // effect count — 0 = clear all effects
    }
}
