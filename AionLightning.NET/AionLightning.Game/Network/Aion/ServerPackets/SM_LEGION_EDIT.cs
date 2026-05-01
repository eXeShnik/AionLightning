using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Notifies clients of a legion modification. Opcode 0x9E.
/// Type 0x00 = level change, 0x03 = contribution points, 0x05 = announcement, 0x06 = disband.
/// </summary>
public sealed class SM_LEGION_EDIT : AionServerPacket
{
    private readonly byte   _type;
    private readonly int    _level;
    private readonly long   _contributionPoints;
    private readonly string _announcement;

    /// <summary>Level change (type 0x00).</summary>
    public SM_LEGION_EDIT(int level) : base(0x9E)
    {
        _type         = 0x00;
        _level        = level;
        _announcement = "";
    }

    /// <summary>Contribution points update (type 0x03).</summary>
    public SM_LEGION_EDIT(long contributionPoints) : base(0x9E)
    {
        _type               = 0x03;
        _contributionPoints = contributionPoints;
        _announcement       = "";
    }

    /// <summary>Announcement update (type 0x05) or disband (type 0x06).</summary>
    public SM_LEGION_EDIT(byte type, string text = "") : base(0x9E)
    {
        _type         = type;
        _announcement = text;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_type);
        switch (_type)
        {
            case 0x00:
                w.WriteC((byte)_level);
                break;
            case 0x03:
                w.WriteQ(_contributionPoints);
                break;
            case 0x05:
                w.WriteS(_announcement);
                w.WriteD((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                break;
            case 0x06:
                w.WriteD((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                break;
        }
    }
}
