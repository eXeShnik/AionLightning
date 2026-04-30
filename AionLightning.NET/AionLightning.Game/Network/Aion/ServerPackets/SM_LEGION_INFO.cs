using AionLightning.Commons.Network;
using AionLightning.Game.Model.Legion;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends full legion metadata to a client. Opcode 0x6E.</summary>
public sealed class SM_LEGION_INFO : AionServerPacket
{
    private readonly Legion _legion;

    public SM_LEGION_INFO(Legion legion) : base(0x6E) => _legion = legion;

    public override void Write(ref PacketWriter w)
    {
        w.WriteS(_legion.Name);
        w.WriteC((byte)_legion.Level);
        w.WriteD(_legion.LegionRank);
        w.WriteH(_legion.DeputyPermission);
        w.WriteH(_legion.CenturionPermission);
        w.WriteH(_legion.LegionaryPermission);
        w.WriteH(_legion.VolunteerPermission);
        w.WriteQ(_legion.ContributionPoints);
        w.WriteD(0);
        w.WriteD(0);
        w.WriteD(0);
        if (!string.IsNullOrEmpty(_legion.Announcement))
        {
            w.WriteS(_legion.Announcement);
            w.WriteD((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
        w.WriteB(new byte[26]);
    }
}
