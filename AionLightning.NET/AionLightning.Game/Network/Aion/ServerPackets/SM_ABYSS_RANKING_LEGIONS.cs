using AionLightning.Commons.Network;
using AionLightning.Game.Dao;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the abyss legion ranking list. Opcode 0x8B.</summary>
public sealed class SM_ABYSS_RANKING_LEGIONS : AionServerPacket
{
    private readonly int                         _wireRaceId;
    private readonly IReadOnlyList<LegionRankEntry> _entries;
    private readonly int                         _updateTime;

    public SM_ABYSS_RANKING_LEGIONS(int wireRaceId, IReadOnlyList<LegionRankEntry> entries) : base(0x8B)
    {
        _wireRaceId = wireRaceId;
        _entries    = entries;
        _updateTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public override void Write(ref PacketWriter w)
    {
        int hasData = _entries.Count > 0 ? 1 : 0;
        w.WriteD(_wireRaceId);   // 0=Elyos, 1=Asmo
        w.WriteD(_updateTime);
        w.WriteD(hasData);
        w.WriteD(hasData);
        w.WriteH((short)_entries.Count);

        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            w.WriteD(i + 1);                   // current rank position
            w.WriteD(i + 1);                   // old rank position (same for now)
            w.WriteD(e.LegionId);
            w.WriteD(_wireRaceId);
            w.WriteC(e.Level);
            w.WriteD(e.MemberCount);
            w.WriteQ(e.ContributionPoints);
            w.WriteS(e.Name, 82);
        }
    }
}
