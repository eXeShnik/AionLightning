using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Notifies player when a friend logs in, logs out, or deletes them. Opcode 0xE1.</summary>
public sealed class SM_FRIEND_NOTIFY : AionServerPacket
{
    public const byte Login   = 0;
    public const byte Logout  = 1;
    public const byte Deleted = 2;

    private readonly string _name;
    private readonly byte   _code;

    public SM_FRIEND_NOTIFY(byte code, string name) : base(0xE1)
    {
        _code = code;
        _name = name;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteS(_name);
        w.WriteC(_code);
    }
}
