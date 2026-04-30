using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Acknowledges macro create (0) or delete (1). Opcode 0xE8.</summary>
public sealed class SM_MACRO_RESULT : AionServerPacket
{
    public static readonly SM_MACRO_RESULT Created = new(0);
    public static readonly SM_MACRO_RESULT Deleted = new(1);

    private readonly byte _code;

    private SM_MACRO_RESULT(byte code) : base(0xE8) => _code = code;

    public override void Write(ref PacketWriter w) => w.WriteC(_code);
}
