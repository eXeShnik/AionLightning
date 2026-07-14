using AionLightning.Commons.Network;
using AionLightning.Game.Model.AutoGroup;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// AutoGroupService queue status/found/enter dialog (Java <c>serverpackets.SM_AUTO_GROUP</c>). Opcode
/// 0x7A (Java <c>ServerPacketsOpcodes</c>: "SM_AUTO_GROUP.class, 0x7A, idSet // 4.5"; unchanged in 4.6).
/// <paramref name="windowId"/> selects the client dialog: 0 request-entry, 1 waiting, 2 cancel-looking,
/// 3 pass, 4 enter-prompt, 5 post-enter-click, 6 entry-icon (battlefield minimap toggle, uses
/// <paramref name="close"/>), 7 failed, 8 on-login resume (uses <paramref name="waitTime"/>).
/// </summary>
public sealed class SM_AUTO_GROUP : AionServerPacket
{
    private readonly int _instanceMaskId;
    private readonly int _mapId;
    private readonly int _messageId;
    private readonly int _titleId;
    private readonly byte _windowId;
    private readonly int _waitTime;
    private readonly bool _close;
    private readonly string _name;

    public SM_AUTO_GROUP(AutoGroupTemplate template, byte windowId, bool close = false, int waitTime = 0, string name = "")
        : base(0x7A)
    {
        _instanceMaskId = template.MaskId;
        _mapId          = template.InstanceMapId;
        _messageId      = template.NameId;
        _titleId        = template.TitleId;
        _windowId       = windowId;
        _close          = close;
        _waitTime       = waitTime;
        _name           = name;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_instanceMaskId);
        w.WriteC(_windowId);
        w.WriteD(_mapId);
        switch (_windowId)
        {
            case 0: // request entry
                w.WriteD(_messageId);
                w.WriteD(_titleId);
                w.WriteD(0);
                break;
            case 1: // waiting window
            case 3: // pass window
            case 8: // on login
                w.WriteD(0);
                w.WriteD(0);
                w.WriteD(_waitTime);
                break;
            case 2: // cancel looking
            case 4: // enter window
            case 5: // after you click enter
                w.WriteD(0);
                w.WriteD(0);
                w.WriteD(0);
                break;
            case 6: // entry icon
                w.WriteD(_messageId);
                w.WriteD(_titleId);
                w.WriteD(_close ? 0 : 1);
                break;
            case 7: // failed window
                w.WriteD(_messageId);
                w.WriteD(_titleId);
                w.WriteD(0);
                break;
            default:
                w.WriteD(0);
                w.WriteD(0);
                w.WriteD(0);
                break;
        }
        w.WriteC(0);
        w.WriteS(_name);
    }
}
