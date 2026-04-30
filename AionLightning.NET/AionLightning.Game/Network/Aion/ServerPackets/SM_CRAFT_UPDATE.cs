using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Updates the crafting progress bar and result. Opcode 0xB5.</summary>
public sealed class SM_CRAFT_UPDATE : AionServerPacket
{
    private readonly int _skillId;
    private readonly int _itemId;
    private readonly int _success;
    private readonly int _failure;
    private readonly int _nameId;
    private readonly int _action;

    /// <param name="action">0=init 1=update 2=bluecrit 3=purplecrit 4=cancel 5=success 6=fail</param>
    public SM_CRAFT_UPDATE(int skillId, int itemId, int nameId, int success, int failure, int action) : base(0xB5)
    {
        _skillId = skillId;
        _itemId  = itemId;
        _nameId  = nameId;
        _success = success;
        _failure = failure;
        _action  = action;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_skillId);
        w.WriteC((byte)_action);
        w.WriteD(_itemId);

        switch (_action)
        {
            case 0: // init
                w.WriteD(_success);
                w.WriteD(_failure);
                w.WriteD(0);
                w.WriteD(1200);
                w.WriteD(1330048);
                w.WriteH(0x24);
                w.WriteD(_nameId);
                w.WriteH(0);
                break;
            case 1: // update
            case 2: // blue crit
            case 5: // success
                w.WriteD(_success);
                w.WriteD(_failure);
                w.WriteD(700);
                w.WriteD(1200);
                w.WriteD(0);
                w.WriteH(0);
                break;
            case 4: // cancel
                w.WriteD(0);
                w.WriteD(0);
                w.WriteD(0);
                w.WriteD(0);
                w.WriteD(1330051);
                w.WriteH(0);
                break;
            case 6: // fail
                w.WriteD(_success);
                w.WriteD(_failure);
                w.WriteD(700);
                w.WriteD(1200);
                w.WriteD(1330050);
                w.WriteH(0x24);
                w.WriteD(_nameId);
                w.WriteH(0);
                break;
            default:
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
