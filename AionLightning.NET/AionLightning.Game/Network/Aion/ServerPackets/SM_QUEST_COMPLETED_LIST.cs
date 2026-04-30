using AionLightning.Commons.Network;
using AionLightning.Game.Model.Quest;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the completed quest list. Opcode 0x7B.</summary>
public sealed class SM_QUEST_COMPLETED_LIST : AionServerPacket
{
    private readonly List<QuestEntry> _quests;

    public SM_QUEST_COMPLETED_LIST(IEnumerable<QuestEntry> quests) : base(0x7B)
        => _quests = quests.ToList();

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(0x01);
        w.WriteH((short)(-_quests.Count & 0xFFFF));
        foreach (var q in _quests)
        {
            w.WriteD(q.QuestId);
            w.WriteC((byte)q.CompleteCount);
            w.WriteC(0);
        }
    }
}
