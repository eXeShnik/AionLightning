using AionLightning.Commons.Network;
using AionLightning.Game.Model.Legion;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Displays a player's legion affiliation and rank above their head. Opcode 0x72.
/// Sent to self on login and to zone peers via CM_LEVEL_READY introduction.
/// </summary>
public sealed class SM_LEGION_UPDATE_TITLE : AionServerPacket
{
    private readonly int _objectId;
    private readonly int _legionId;
    private readonly string _legionName;
    private readonly byte _rankDisplay;

    // Java rank display: BrigadeGeneral→0, Deputy/Centurion→1, Legionary/Volunteer→2
    public SM_LEGION_UPDATE_TITLE(int objectId, int legionId, string legionName, LegionRank rank)
        : base(0x72)
    {
        _objectId   = objectId;
        _legionId   = legionId;
        _legionName = legionName;
        _rankDisplay = rank switch
        {
            LegionRank.BrigadeGeneral => 0,
            LegionRank.Deputy or LegionRank.Centurion => 1,
            _ => 2,
        };
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        w.WriteD(_legionId);
        w.WriteS(_legionName);
        w.WriteC(_rankDisplay);
    }
}
