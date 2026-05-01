using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the player's abyss rank and AP/kill statistics. Opcode 0xED.</summary>
public sealed class SM_ABYSS_RANK : AionServerPacket
{
    private readonly long _ap;
    private readonly int  _rank;
    private readonly int  _maxRank;
    private readonly int  _allKill;
    private readonly int  _dailyKill;
    private readonly long _dailyAp;
    private readonly int  _weeklyKill;
    private readonly long _weeklyAp;
    private readonly int  _lastKill;
    private readonly long _lastAp;

    public SM_ABYSS_RANK(long ap, int rank, int maxRank = 1,
        int allKill = 0, int dailyKill = 0, long dailyAp = 0,
        int weeklyKill = 0, long weeklyAp = 0,
        int lastKill = 0, long lastAp = 0) : base(0xED)
    {
        _ap          = ap;
        _rank        = rank;
        _maxRank     = Math.Max(rank, maxRank);
        _allKill     = allKill;
        _dailyKill   = dailyKill;
        _dailyAp     = dailyAp;
        _weeklyKill  = weeklyKill;
        _weeklyAp    = weeklyAp;
        _lastKill    = lastKill;
        _lastAp      = lastAp;
    }

    public static SM_ABYSS_RANK ForPlayer(Player p) =>
        new(p.AbyssPoints, p.AbyssRank, p.AbyssMaxRank,
            p.AbyssAllKill, p.AbyssDailyKill, p.AbyssDailyAp,
            p.AbyssWeeklyKill, p.AbyssWeeklyAp, p.AbyssLastKill, p.AbyssLastAp);

    public override void Write(ref PacketWriter w)
    {
        long nextThreshold = AbyssRankService.GetNextRankThreshold(_rank);
        int progress = _rank < 9
            ? (int)Math.Min(100, 100L * _ap / nextThreshold)
            : 100;

        w.WriteQ(_ap);
        w.WriteD(0);           // GP (glory points — ranks 10+ not implemented)
        w.WriteD(_rank);
        w.WriteD(0);           // top ranking position
        w.WriteD(progress);
        w.WriteD(_allKill);
        w.WriteD(_maxRank);
        w.WriteD(_dailyKill);
        w.WriteQ(_dailyAp);
        w.WriteD(0);           // daily GP
        w.WriteD(_weeklyKill);
        w.WriteQ(_weeklyAp);
        w.WriteD(0);           // weekly GP
        w.WriteD(_lastKill);
        w.WriteQ(_lastAp);
        w.WriteD(0);           // last-week GP
        w.WriteC(0);           // unk
    }
}
