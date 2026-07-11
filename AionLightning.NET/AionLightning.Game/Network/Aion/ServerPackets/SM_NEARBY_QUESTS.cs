using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends the quests available to start near the player's current position (Java parity:
/// <c>PlayerController.updateNearbyQuests</c>). Sent on enter world and on level-up. Opcode 0x7F.
/// </summary>
public sealed class SM_NEARBY_QUESTS : AionServerPacket
{
    private readonly IReadOnlyList<(int QuestId, int LevelDiff)> _nearbyQuests;

    public SM_NEARBY_QUESTS(IReadOnlyList<(int QuestId, int LevelDiff)> nearbyQuests) : base(0x7F)
        => _nearbyQuests = nearbyQuests;

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0);
        // Java quirk, ported exactly: the entry count is written as a negated 16-bit value.
        w.WriteH((short)(-_nearbyQuests.Count & 0xFFFF));
        foreach (var (questId, levelDiff) in _nearbyQuests)
        {
            if (levelDiff > 0)
            {
                w.WriteH(questId);
                w.WriteH(0x02); // grey icon for quests not yet available (future)
            }
            else
            {
                // Quests are displayed on the map
                w.WriteD(questId);
            }
        }
    }
}
