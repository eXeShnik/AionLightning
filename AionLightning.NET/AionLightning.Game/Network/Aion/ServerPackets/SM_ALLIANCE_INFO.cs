using AionLightning.Commons.Network;
using AionLightning.Game.Model.Alliance;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Alliance metadata packet. Opcode 0xF5 (Java 4.5 table value — TODO: verify opcode vs live 4.6 client).
/// Sent to every member on alliance creation/join/leave, loot-rule change, captain/vice-captain change,
/// and league state changes. Wire format matches Java <c>SM_ALLIANCE_INFO.writeImpl()</c> exactly,
/// including the league tail appended when the alliance is leagued.
/// </summary>
public sealed class SM_ALLIANCE_INFO : AionServerPacket
{
    private readonly PlayerAlliance _alliance;
    private readonly int _messageId;
    private readonly string _message;

    public SM_ALLIANCE_INFO(PlayerAlliance alliance) : this(alliance, 0, string.Empty) { }

    public SM_ALLIANCE_INFO(PlayerAlliance alliance, int messageId, string message) : base(0xF5)
    {
        _alliance  = alliance;
        _messageId = messageId;
        _message   = message;
    }

    public override void Write(ref PacketWriter w)
    {
        var leader = _alliance.GetMember(_alliance.CaptainObjectId);

        // Java writes alliance.groupSize(), which is the fixed subgroup count (always 4), not the
        // member count.
        w.WriteH((ushort)_alliance.Groups.Count);
        w.WriteD(_alliance.AllianceId);
        w.WriteD(_alliance.CaptainObjectId);
        // note: Java writes the *receiving* connection's player.getWorldId(); AionServerPacket.Write has
        // no per-connection context in this port, so the captain's worldId is used instead — the same
        // approximation SM_GROUP_INFO already makes for the party equivalent of this field.
        w.WriteD(leader?.Player.Position.WorldId ?? 0);

        foreach (var viceCaptainId in _alliance.ViceCaptainIds)
            w.WriteD(viceCaptainId);
        for (int i = _alliance.ViceCaptainIds.Count; i < PlayerAlliance.MaxViceCaptains; i++)
            w.WriteD(0);

        w.WriteD(_alliance.LootDistribution);
        w.WriteD(_alliance.LootMisc);
        w.WriteD(_alliance.CommonItemAbove);
        w.WriteD(_alliance.SuperiorItemAbove);
        w.WriteD(_alliance.HeroicItemAbove);
        w.WriteD(_alliance.FabledItemAbove);
        w.WriteD(_alliance.EthernalItemAbove);
        w.WriteD(_alliance.AutoDistribution);
        w.WriteD(2);                               // constant in Java
        w.WriteC(0);
        w.WriteD(1);                               // TeamType.REGULAR — sieges/vortex team types not ported
        w.WriteD(0);                               // TeamType subType
        w.WriteD(_alliance.IsInLeague ? _alliance.League!.LeagueId : 0);

        for (int i = 0; i < 4; i++)
        {
            w.WriteD(i);       // group num
            w.WriteD(1000 + i); // group id
        }

        w.WriteD(_messageId);
        w.WriteS(_messageId != 0 ? _message : string.Empty);

        if (_alliance.IsInLeague)
        {
            var league = _alliance.League!;
            w.WriteH((ushort)league.Members.Count);
            w.WriteD(league.LootDistribution);
            w.WriteD(league.AutoDistribution);
            w.WriteD(league.CommonItemAbove);
            w.WriteD(league.SuperiorItemAbove);
            w.WriteD(league.HeroicItemAbove);
            w.WriteD(league.FabledItemAbove);
            w.WriteD(league.EthernalItemAbove);
            w.WriteD(0); // "over_ethernal" in Java — always zero there too
            w.WriteD(0); // "over_over_ethernal" in Java — always zero there too

            foreach (var member in league.SortedMembers)
            {
                var memberLeader = member.Alliance.GetMember(member.Alliance.CaptainObjectId);
                w.WriteD(member.Position);
                w.WriteD(member.AllianceId);
                w.WriteD(member.Alliance.MemberCount);
                w.WriteS(memberLeader?.Name ?? string.Empty);
            }
        }
    }
}
