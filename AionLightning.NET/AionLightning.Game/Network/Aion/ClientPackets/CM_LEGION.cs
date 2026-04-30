using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model.Legion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_LEGION : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly LegionService            _legionService;
    private readonly ILegionDao               _legionDao;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int    _exOpcode;
    private string _legionName   = "";
    private string _charName     = "";
    private string _announcement = "";

    public CM_LEGION(GsClientConnection conn, LegionService legionService,
        ILegionDao legionDao, PlayerConnectionRegistry connRegistry)
    {
        _conn          = conn;
        _legionService = legionService;
        _legionDao     = legionDao;
        _connRegistry  = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _exOpcode = r.ReadC();
        switch (_exOpcode)
        {
            case 0x00: r.ReadD(); _legionName   = r.ReadS(); break;
            case 0x01: r.ReadD(); _charName     = r.ReadS(); break;
            case 0x02: r.ReadD(); r.ReadH();                 break;
            case 0x04: r.ReadD(); _charName     = r.ReadS(); break;
            case 0x05: r.ReadD(); _charName     = r.ReadS(); break;
            case 0x06: r.ReadD(); _charName     = r.ReadS(); break;
            case 0x07: r.ReadD(); _charName     = r.ReadS(); break;
            case 0x08: r.ReadD(); r.ReadH();                 break;
            case 0x09: r.ReadD(); _announcement = r.ReadS(); break;
            case 0x0A: r.ReadD(); r.ReadS();                 break;
            case 0x0D: r.ReadH(); r.ReadH(); r.ReadH(); r.ReadH(); break;
            case 0x0E: r.ReadD(); r.ReadH();                 break;
            case 0x0F: r.ReadS(); r.ReadS();                 break;
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        switch (_exOpcode)
        {
            case 0x00:
                await HandleCreateAsync(player, ct);
                break;
            case 0x01:
                await HandleInviteAsync(player, ct);
                break;
            case 0x02:
                await HandleLeaveAsync(player, ct);
                break;
            case 0x04:
                await HandleKickAsync(player, ct);
                break;
            case 0x05:
                await HandleRankChangeAsync(player, LegionRank.BrigadeGeneral, ct);
                break;
            case 0x06:
                await HandleRankChangeAsync(player, LegionRank.Centurion, ct);
                break;
            case 0x07:
                await HandleRankChangeAsync(player, LegionRank.Legionary, ct);
                break;
            case 0x09:
                await HandleAnnouncementAsync(player, ct);
                break;
        }
    }

    private async ValueTask HandleCreateAsync(Model.Player player, CancellationToken ct)
    {
        if (player.Legion is not null) return;
        if (string.IsNullOrWhiteSpace(_legionName)) return;
        if (_legionService.GetByName(_legionName) is not null) return;

        var existingInDb = await _legionDao.GetByNameAsync(_legionName, ct);
        if (existingInDb is not null) return;

        int legionId = await _legionDao.CreateLegionAsync(_legionName, ct);
        await _legionDao.AddMemberAsync(legionId, player.ObjectId, player.Name,
            (int)player.PlayerClass, player.Level, player.Position.WorldId, LegionRank.BrigadeGeneral, ct);

        var legion = new Legion { LegionId = legionId, Name = _legionName };
        var member = new LegionMember
        {
            ObjectId = player.ObjectId,
            Name     = player.Name,
            Rank     = LegionRank.BrigadeGeneral,
            ClassId  = (int)player.PlayerClass,
            Level    = player.Level,
            WorldId  = player.Position.WorldId,
            IsOnline = true,
        };
        legion.Members[player.ObjectId] = member;
        _legionService.AddLegion(legion);
        player.Legion = legion;

        await _conn.SendAsync(new SM_LEGION_INFO(legion), ct);
        await _conn.SendAsync(new SM_LEGION_MEMBERLIST(legion.Members.Values), ct);
    }

    private async ValueTask HandleInviteAsync(Model.Player player, CancellationToken ct)
    {
        var legion = player.Legion;
        if (legion is null) return;
        if (!legion.Members.TryGetValue(player.ObjectId, out var inviterMember)) return;
        if (inviterMember.Rank != LegionRank.BrigadeGeneral) return;

        var targetConn = _connRegistry.GetByName(_charName);
        var target = targetConn?.ActivePlayer;
        if (target is null) return;
        if (target.Legion is not null) return;

        var newMember = new LegionMember
        {
            ObjectId = target.ObjectId,
            Name     = target.Name,
            Rank     = LegionRank.Volunteer,
            ClassId  = (int)target.PlayerClass,
            Level    = target.Level,
            WorldId  = target.Position.WorldId,
            IsOnline = true,
        };

        await _legionDao.AddMemberAsync(legion.LegionId, target.ObjectId, target.Name,
            (int)target.PlayerClass, target.Level, target.Position.WorldId, LegionRank.Volunteer, ct);

        _legionService.AddMember(legion, newMember);
        target.Legion = legion;

        await targetConn!.SendAsync(new SM_LEGION_INFO(legion), ct);
        await targetConn.SendAsync(new SM_LEGION_MEMBERLIST(legion.Members.Values), ct);

        var addPkt = new SM_LEGION_ADD_MEMBER(target, newMember);
        foreach (var m in legion.Members.Values)
        {
            if (m.ObjectId == target.ObjectId) continue;
            var mc = _connRegistry.Get(m.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(addPkt, ct); } catch { }
        }
    }

    private async ValueTask HandleLeaveAsync(Model.Player player, CancellationToken ct)
    {
        var legion = player.Legion;
        if (legion is null) return;

        await RemoveMemberFromLegionAsync(legion, player, ct);
    }

    private async ValueTask HandleKickAsync(Model.Player player, CancellationToken ct)
    {
        var legion = player.Legion;
        if (legion is null) return;
        if (!legion.Members.TryGetValue(player.ObjectId, out var kickerMember)) return;
        if (kickerMember.Rank != LegionRank.BrigadeGeneral) return;

        var targetConn = _connRegistry.GetByName(_charName);
        var target = targetConn?.ActivePlayer;
        if (target is null || target.Legion?.LegionId != legion.LegionId) return;

        await RemoveMemberFromLegionAsync(legion, target, ct);
    }

    private async ValueTask RemoveMemberFromLegionAsync(Legion legion, Model.Player player, CancellationToken ct)
    {
        _legionService.RemoveMember(legion, player.ObjectId);
        await _legionDao.RemoveMemberAsync(player.ObjectId, ct);
        player.Legion = null;

        var leavePkt = new SM_LEGION_LEAVE_MEMBER(player.ObjectId, player.Name);
        foreach (var m in legion.Members.Values)
        {
            var mc = _connRegistry.Get(m.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(leavePkt, ct); } catch { }
        }

        if (legion.Members.Count == 0)
        {
            _legionService.RemoveLegion(legion);
            await _legionDao.DeleteLegionAsync(legion.LegionId, ct);
        }
    }

    private async ValueTask HandleRankChangeAsync(Model.Player player, LegionRank newRank, CancellationToken ct)
    {
        var legion = player.Legion;
        if (legion is null) return;
        if (!legion.Members.TryGetValue(player.ObjectId, out var actorMember)) return;
        if (actorMember.Rank != LegionRank.BrigadeGeneral) return;

        var targetConn = _connRegistry.GetByName(_charName);
        var target = targetConn?.ActivePlayer;
        if (target is null || target.Legion?.LegionId != legion.LegionId) return;
        if (!legion.Members.TryGetValue(target.ObjectId, out var targetMember)) return;

        targetMember.Rank = newRank;
        await _legionDao.UpdateRankAsync(target.ObjectId, newRank, ct);

        var updatePkt = new SM_LEGION_ADD_MEMBER(target, targetMember);
        foreach (var m in legion.Members.Values)
        {
            var mc = _connRegistry.Get(m.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(updatePkt, ct); } catch { }
        }
    }

    private async ValueTask HandleAnnouncementAsync(Model.Player player, CancellationToken ct)
    {
        var legion = player.Legion;
        if (legion is null) return;
        if (!legion.Members.TryGetValue(player.ObjectId, out var member)) return;
        if (member.Rank != LegionRank.BrigadeGeneral && member.Rank != LegionRank.Deputy) return;

        legion.Announcement = _announcement;
        await _legionDao.UpdateAnnouncementAsync(legion.LegionId, _announcement, ct);

        var editPkt = new SM_LEGION_EDIT(0x05, _announcement);
        foreach (var m in legion.Members.Values)
        {
            var mc = _connRegistry.Get(m.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(editPkt, ct); } catch { }
        }
    }
}
