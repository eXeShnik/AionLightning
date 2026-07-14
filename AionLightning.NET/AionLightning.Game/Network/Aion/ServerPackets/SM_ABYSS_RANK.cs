using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the player's abyss rank and AP/kill statistics. Opcode 0xED.</summary>
public sealed class SM_ABYSS_RANK : AionServerPacket
{
    private readonly long _ap;
    private readonly long _gp;
    private readonly int  _rank;
    private readonly int  _maxRank;
    private readonly int  _topRanking;
    private readonly int  _allKill;
    private readonly int  _dailyKill;
    private readonly long _dailyAp;
    private readonly long _dailyGp;
    private readonly int  _weeklyKill;
    private readonly long _weeklyAp;
    private readonly long _weeklyGp;
    private readonly int  _lastKill;
    private readonly long _lastAp;
    private readonly long _lastGp;

    public SM_ABYSS_RANK(long ap, int rank, int maxRank = 1, long gp = 0, int topRanking = 0,
        int allKill = 0, int dailyKill = 0, long dailyAp = 0, long dailyGp = 0,
        int weeklyKill = 0, long weeklyAp = 0, long weeklyGp = 0,
        int lastKill = 0, long lastAp = 0, long lastGp = 0) : base(0xED)
    {
        _ap          = ap;
        _gp          = gp;
        _rank        = rank;
        _maxRank     = Math.Max(rank, maxRank);
        _topRanking  = topRanking;
        _allKill     = allKill;
        _dailyKill   = dailyKill;
        _dailyAp     = dailyAp;
        _dailyGp     = dailyGp;
        _weeklyKill  = weeklyKill;
        _weeklyAp    = weeklyAp;
        _weeklyGp    = weeklyGp;
        _lastKill    = lastKill;
        _lastAp      = lastAp;
        _lastGp      = lastGp;
    }

    public static SM_ABYSS_RANK ForPlayer(Player p) =>
        new(p.AbyssPoints, p.AbyssRank, p.AbyssMaxRank, p.AbyssGp, p.AbyssTopRanking,
            p.AbyssAllKill, p.AbyssDailyKill, p.AbyssDailyAp, p.AbyssDailyGp,
            p.AbyssWeeklyKill, p.AbyssWeeklyAp, p.AbyssWeeklyGp,
            p.AbyssLastKill, p.AbyssLastAp, p.AbyssLastGp);

    public override void Write(ref PacketWriter w)
    {
        long nextThreshold = AbyssRankService.GetNextRankThreshold(_rank);
        // Ranks 1-8 progress toward the next AP threshold; rank 9+ progresses toward the next GP
        // threshold (officer promotion); rank 18 (max) always shows full progress.
        long progressValue = _rank < 9 ? _ap : _gp;
        int progress = _rank < 18
            ? (int)Math.Min(100, 100L * progressValue / nextThreshold)
            : 100;

        w.WriteQ(_ap);
        w.WriteD((int)_gp);
        w.WriteD(_rank);
        w.WriteD(_topRanking);
        w.WriteD(progress);
        w.WriteD(_allKill);
        w.WriteD(_maxRank);
        w.WriteD(_dailyKill);
        w.WriteQ(_dailyAp);
        w.WriteD((int)_dailyGp);
        w.WriteD(_weeklyKill);
        w.WriteQ(_weeklyAp);
        w.WriteD((int)_weeklyGp);
        w.WriteD(_lastKill);
        w.WriteQ(_lastAp);
        w.WriteD((int)_lastGp);
        w.WriteC(0);           // unk
    }
}
