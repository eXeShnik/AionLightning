using AionLightning.Commons.Network;
using AionLightning.Game.Dao;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends a page of the abyss player ranking list. Opcode 0x8A.</summary>
public sealed class SM_ABYSS_RANKING_PLAYERS : AionServerPacket
{
    private readonly int                      _wireRaceId;
    private readonly int                      _lastUpdate;
    private readonly IReadOnlyList<AbyssRankEntry> _entries;

    public SM_ABYSS_RANKING_PLAYERS(int wireRaceId, IReadOnlyList<AbyssRankEntry> entries) : base(0x8A)
    {
        _wireRaceId = wireRaceId;
        _entries    = entries;
        _lastUpdate = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_wireRaceId);   // 0=Elyos, 1=Asmo (wire format)
        w.WriteD(_lastUpdate);   // timestamp
        w.WriteD(0);             // page (0-based)
        w.WriteD(0x7F);          // isEndPacket (0x7F = final page)
        w.WriteH((short)_entries.Count);

        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            w.WriteD(i + 1);                // rank position (1-based)
            w.WriteD(e.AbyssRank);          // abyss rank level (1-9)
            w.WriteD(i + 1);                // old rank position (same as current for now)
            w.WriteD(e.PlayerId);           // player id
            w.WriteD(_wireRaceId);          // race again
            w.WriteD((int)e.Class);         // class id
            w.WriteD(0);                    // sex (0=male — not tracked in ranking)
            w.WriteQ(e.AbyssPoints);        // abyss points (8 bytes)
            w.WriteD(0);                    // glory points (not implemented)
            w.WriteH((short)e.Level);       // level
            w.WriteS(e.Name, 52);           // player name (padded to 52 bytes)
            w.WriteS(e.LegionName, 82);     // legion name (padded to 82 bytes)
            w.WriteD(0);                    // unk
        }
    }
}
