using AionLightning.Commons.Network;
using AionLightning.Game.Model.Quest;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the active quest list. Opcode 0x47.</summary>
public sealed class SM_QUEST_LIST : AionServerPacket
{
    private readonly List<QuestEntry> _quests;

    public SM_QUEST_LIST(IEnumerable<QuestEntry> quests) : base(0x47)
        => _quests = quests.ToList();

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(0x01);
        w.WriteH((short)(-_quests.Count & 0xFFFF));
        foreach (var q in _quests)
        {
            w.WriteD(q.QuestId);
            w.WriteC((byte)q.Status);
            w.WriteD(q.Step);
            w.WriteC((byte)q.CompleteCount);
        }
    }
}
