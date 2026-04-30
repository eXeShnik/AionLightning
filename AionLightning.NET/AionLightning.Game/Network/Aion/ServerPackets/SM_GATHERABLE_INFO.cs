using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Introduces a gatherable object to the client. Opcode 0x17.</summary>
public sealed class SM_GATHERABLE_INFO : AionServerPacket
{
    private readonly Gatherable _gatherable;

    public SM_GATHERABLE_INFO(Gatherable gatherable) : base(0x17) => _gatherable = gatherable;

    public override void Write(ref PacketWriter w)
    {
        w.WriteF(_gatherable.Position.X);
        w.WriteF(_gatherable.Position.Y);
        w.WriteF(_gatherable.Position.Z);
        w.WriteD(_gatherable.ObjectId);
        w.WriteD(_gatherable.ObjectId);                        // staticId — use objectId as unique spawn identifier
        w.WriteD(_gatherable.Template.TemplateId);
        w.WriteH(1);                                           // status: 1 = available
        w.WriteC((byte)_gatherable.Position.Heading);
        w.WriteD(_gatherable.Template.NameId);
        w.WriteH(0);
        w.WriteH(0);
        w.WriteH(0);
        w.WriteC(100);                                         // unk
    }
}
