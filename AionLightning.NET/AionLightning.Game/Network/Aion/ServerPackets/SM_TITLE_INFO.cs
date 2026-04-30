using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends title information. Opcode 0xB0.</summary>
public sealed class SM_TITLE_INFO : AionServerPacket
{
    private readonly byte _action;
    private readonly int  _titleId;

    /// <summary>action=0: sends the full title list (empty for new players).</summary>
    public static SM_TITLE_INFO EmptyList()          => new(0, -1);

    /// <summary>action=1: sets the player's active title.</summary>
    public static SM_TITLE_INFO ActiveTitle(int id)  => new(1, id);

    /// <summary>action=6: sets the player's bonus title.</summary>
    public static SM_TITLE_INFO BonusTitle(int id)   => new(6, id);

    private SM_TITLE_INFO(byte action, int titleId) : base(0xB0)
    {
        _action  = action;
        _titleId = titleId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        switch (_action)
        {
            case 0:
                w.WriteC(0);   // unk
                w.WriteH(0);   // title count = 0
                break;
            case 1:
            case 4:
                w.WriteH((short)(_titleId < 0 ? -1 : _titleId));
                break;
            case 6:
                w.WriteH((short)(_titleId < 0 ? -1 : _titleId));
                break;
        }
    }
}
