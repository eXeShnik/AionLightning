using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends title information. Opcode 0xB0.</summary>
public sealed class SM_TITLE_INFO : AionServerPacket
{
    private readonly byte _action;
    private readonly int  _titleId;
    private readonly int  _objectId;

    /// <summary>action=0: sends the full title list (empty for new players).</summary>
    public static SM_TITLE_INFO EmptyList()                                 => new(0, 0, -1);

    /// <summary>action=1: sets the player's own active title (self-notify).</summary>
    public static SM_TITLE_INFO ActiveTitle(int id)                         => new(1, 0, id);

    /// <summary>action=3: broadcasts a player's active title to zone peers.</summary>
    public static SM_TITLE_INFO BroadcastTitle(int objectId, int titleId)   => new(3, objectId, titleId);

    /// <summary>action=6: sets the player's own bonus title (self-notify).</summary>
    public static SM_TITLE_INFO BonusTitle(int id)                          => new(6, 0, id);

    private SM_TITLE_INFO(byte action, int objectId, int titleId) : base(0xB0)
    {
        _action   = action;
        _objectId = objectId;
        _titleId  = titleId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        switch (_action)
        {
            case 0:
                w.WriteC(0);
                w.WriteH(0);   // title count = 0
                break;
            case 1:
            case 4:
                w.WriteH((short)(_titleId < 0 ? -1 : _titleId));
                break;
            case 3:
                w.WriteD(_objectId);
                w.WriteH((short)(_titleId < 0 ? -1 : _titleId));
                break;
            case 6:
                w.WriteH((short)(_titleId < 0 ? -1 : _titleId));
                break;
        }
    }
}
