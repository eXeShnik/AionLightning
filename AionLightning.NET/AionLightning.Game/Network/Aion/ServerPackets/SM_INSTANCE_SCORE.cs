using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Instance;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Instance scoreboard packet (Java <c>network.aion.serverpackets.SM_INSTANCE_SCORE</c>). Opcode
/// 0x79 (4.5 table — TODO: verify opcode vs live 4.6 client). Java's writeImpl branches on the
/// instance's mapId to serialize a dozen unrelated scoreboard layouts (arenas, crucible, dark poeta,
/// legion instances, ...); only the Dredgion branch (Baranath 300110000 / Chantra 300210000 /
/// Terath 300440000 — <c>fillTableWithGroup</c> + room-capture byte array) is ported here, since
/// those are the only instances this port's scoring framework covers.
/// </summary>
public sealed class SM_INSTANCE_SCORE : AionServerPacket
{
    private readonly int _instanceTime;
    private readonly DredgionReward _reward;
    private readonly IReadOnlyList<Player> _players;

    public SM_INSTANCE_SCORE(int instanceTime, DredgionReward reward, IReadOnlyList<Player> players) : base(0x79)
    {
        _instanceTime = instanceTime;
        _reward       = reward;
        _players      = players;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_reward.MapId);
        w.WriteD(_instanceTime);
        w.WriteD((int)_reward.ScoreType);

        FillTableWithGroup(ref w, Race.ELYOS);
        FillTableWithGroup(ref w, Race.ASMODIANS);

        int elyosScore = _reward.GetPointsByRace(Race.ELYOS);
        int asmoScore  = _reward.GetPointsByRace(Race.ASMODIANS);
        w.WriteD(_reward.ScoreType.IsEndProgress() ? (asmoScore > elyosScore ? 1 : 0) : 255);
        w.WriteD(elyosScore);
        w.WriteD(asmoScore);
        w.WriteH(0);
        foreach (var room in _reward.Rooms)
            w.WriteC(room.State);
    }

    private void FillTableWithGroup(ref PacketWriter w, Race race)
    {
        int count = 0;
        foreach (var player in _players)
        {
            if (player.Race != race) continue;
            var reward = _reward.GetPlayerReward(player.ObjectId);
            if (reward is null) continue;

            w.WriteD(player.ObjectId);
            w.WriteD(player.AbyssRank);
            w.WriteD(reward.PvPKills);
            w.WriteD(reward.MonsterKills);
            w.WriteD(reward.ZoneCaptured);
            w.WriteD(reward.Points);
            if (_reward.ScoreType.IsEndProgress())
            {
                bool winner = race == _reward.WinningRace;
                int basePoints = winner ? _reward.WinnerPoints : _reward.LooserPoints;
                w.WriteD(basePoints + (int)(reward.Points * 1.6f));
                w.WriteD(basePoints);
            }
            else
            {
                w.WriteZero(8);
            }
            w.WriteC((byte)player.PlayerClass);
            w.WriteC(0);
            w.WriteS(player.Name, 54);
            count++;
        }
        if (count < 6) w.WriteZero(88 * (6 - count));
    }
}
