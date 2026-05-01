using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends title information. Opcode 0xB0.</summary>
public sealed class SM_TITLE_INFO : AionServerPacket
{
    private readonly byte             _action;
    private readonly int              _titleId;
    private readonly int              _objectId;
    private readonly IReadOnlyList<int>? _titles;

    /// <summary>action=0: sends the full owned title list.</summary>
    public static SM_TITLE_INFO TitleList(IEnumerable<int> titles)          => new(0, titles.ToList());

    /// <summary>action=1: sets the player's own active title (self-notify).</summary>
    public static SM_TITLE_INFO ActiveTitle(int id)                         => new(1, 0, id);

    /// <summary>action=3: broadcasts a player's active title to zone peers.</summary>
    public static SM_TITLE_INFO BroadcastTitle(int objectId, int titleId)   => new(3, objectId, titleId);

    /// <summary>action=4: notifies client of a newly acquired title.</summary>
    public static SM_TITLE_INFO AddTitle(int id)                            => new(4, 0, id);

    /// <summary>action=6: sets the player's own bonus title (self-notify).</summary>
    public static SM_TITLE_INFO BonusTitle(int id)                          => new(6, 0, id);

    private SM_TITLE_INFO(byte action, int objectId, int titleId) : base(0xB0)
    {
        _action   = action;
        _objectId = objectId;
        _titleId  = titleId;
    }

    private SM_TITLE_INFO(byte action, IReadOnlyList<int> titles) : base(0xB0)
    {
        _action  = action;
        _titles  = titles;
        _objectId = 0;
        _titleId  = 0;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        switch (_action)
        {
            case 0:
                w.WriteC(0);
                w.WriteH((short)(_titles?.Count ?? 0));
                if (_titles is not null)
                    foreach (var t in _titles)
                        w.WriteH((short)t);
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
