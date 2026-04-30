using AionLightning.Commons.Network;
using AionLightning.Game.Model.Templates.Gatherable;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Gather progress update — shows skill bar and item info. Opcode 0xA5.</summary>
public sealed class SM_GATHER_UPDATE : AionServerPacket
{
    private readonly GatherableTemplate  _template;
    private readonly GatherableMaterial? _material;
    private readonly int                 _success;
    private readonly int                 _failure;
    private readonly int                 _action;

    public SM_GATHER_UPDATE(GatherableTemplate template, GatherableMaterial? material,
        int success, int failure, int action) : base(0xA5)
    {
        _template = template;
        _material = material;
        _success  = success;
        _failure  = failure;
        _action   = action;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_template.HarvestSkill);
        w.WriteC((byte)_action);
        w.WriteD(_material?.ItemId ?? 0);

        switch (_action)
        {
            case 0: // start gathering — show skill/item popup
                w.WriteD(_success);
                w.WriteD(_failure);
                w.WriteD(0);
                w.WriteD(1200);
                w.WriteD(1330011);
                w.WriteH(0x24);
                w.WriteD(_material?.NameId ?? 0);
                w.WriteH(0);
                break;

            case 7: // item acquired
                w.WriteD(_success);
                w.WriteD(_failure);
                w.WriteD(0);
                w.WriteD(1200);
                w.WriteD(1330079);
                w.WriteH(0x24);
                w.WriteD(_material?.NameId ?? 0);
                w.WriteH(0);
                break;

            default: // generic update / stop
                w.WriteD(_success);
                w.WriteD(_failure);
                w.WriteD(700);
                w.WriteD(1200);
                w.WriteD(0);
                w.WriteH(0);
                break;
        }
    }
}
