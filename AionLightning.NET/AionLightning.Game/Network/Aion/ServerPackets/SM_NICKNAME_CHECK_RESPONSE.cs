using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Response to CM_CHECK_NICKNAME. Opcode 0xE9.
/// value: 0 = name available, 10 = name already in use.
/// </summary>
public sealed class SM_NICKNAME_CHECK_RESPONSE : AionServerPacket
{
    private readonly byte _value;

    public SM_NICKNAME_CHECK_RESPONSE(byte value) : base(0xE9) => _value = value;

    public override void Write(ref PacketWriter w) => w.WriteC(_value);
}
