using AionLightning.Commons.Network;
using AionLightning.Game.Model.Legion;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Notifies clients of a legion modification. Opcode 0x9E.
/// Type 0x00 = level change, 0x02 = permissions, 0x03 = contribution points, 0x04 = warehouse kinah, 0x05 = announcement, 0x06 = disband.
/// </summary>
public sealed class SM_LEGION_EDIT : AionServerPacket
{
    private readonly byte   _type;
    private readonly int    _level;
    private readonly long   _contributionPoints;
    private readonly long   _warehouseKinah;
    private readonly string _announcement;
    private readonly short  _deputyPermission;
    private readonly short  _centurionPermission;
    private readonly short  _legionaryPermission;
    private readonly short  _volunteerPermission;

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

    private SM_LEGION_EDIT(long kinah, bool isWarehouseKinah) : base(0x9E)
    {
        _type           = 0x04;
        _warehouseKinah = kinah;
        _announcement   = "";
    }

    /// <summary>Warehouse kinah update (type 0x04).</summary>
    public static SM_LEGION_EDIT WarehouseKinah(long kinah) => new(kinah, isWarehouseKinah: true);

    private SM_LEGION_EDIT(short deputy, short centurion, short legionary, short volunteer) : base(0x9E)
    {
        _type                = 0x02;
        _deputyPermission    = deputy;
        _centurionPermission = centurion;
        _legionaryPermission = legionary;
        _volunteerPermission = volunteer;
        _announcement        = "";
    }

    /// <summary>Permissions update (type 0x02).</summary>
    public static SM_LEGION_EDIT Permissions(Legion legion) =>
        new(legion.DeputyPermission, legion.CenturionPermission, legion.LegionaryPermission, legion.VolunteerPermission);

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
            case 0x02:
                w.WriteH(_deputyPermission);
                w.WriteH(_centurionPermission);
                w.WriteH(_legionaryPermission);
                w.WriteH(_volunteerPermission);
                break;
            case 0x03:
                w.WriteQ(_contributionPoints);
                break;
            case 0x04:
                w.WriteQ(_warehouseKinah);
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
