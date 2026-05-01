using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_REVIVE : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager             _dataManager;
    private readonly IPlayerDao               _playerDao;

    public CM_REVIVE(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        IDataManager dataManager, IPlayerDao playerDao)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _dataManager  = dataManager;
        _playerDao    = playerDao;
    }

    public override void Read(ref PacketReader r) => r.ReadC(); // reviveId (unused for basic bind revive)

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || !player.IsAlreadyDead) return;

        // Apply soul sickness (bind revive increments death count, max 10) — mirrors Java PlayerReviveService.revive
        if (player.SoulSicknessCount < 10)
        {
            player.SoulSicknessCount++;
            await _playerDao.UpdateSoulSicknessAsync(player.ObjectId, player.SoulSicknessCount, ct);
        }

        // Recompute MaxHp/MaxMp with the updated soul sickness penalty before restoring HP/MP
        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        float ssMult = player.SoulSicknessMultiplier;
        player.MaxHp = (int)(((statTpl?.MaxHp ?? 1000) + player.BonusMaxHp + player.TitleBonusMaxHp) * ssMult);
        player.MaxMp = (int)(((statTpl?.MaxMp ?? 500)  + player.BonusMaxMp + player.TitleBonusMaxMp) * ssMult);

        // Restore to 25 % HP / MP; drain DP to 0 (mirrors Java PlayerReviveService.revive)
        player.CurrentHp = Math.Max(1, player.MaxHp / 4);
        player.CurrentMp = Math.Max(1, player.MaxMp / 4);
        player.State &= ~CreatureState.Dead;
        if (player.Dp > 0)
        {
            player.Dp = 0;
            await _playerDao.UpdateDpAsync(player.ObjectId, 0, ct);
            await _conn.SendAsync(new SM_DP_INFO(player.ObjectId, 0), ct);
        }

        // Determine destination position
        Position destination;
        if (player.BindPosition.HasValue)
        {
            destination = player.BindPosition.Value;
        }
        else
        {
            var spawn = _dataManager.PlayerInitial.GetSpawnLocation(player.Race);
            destination = new Position(spawn.X, spawn.Y, spawn.Z, spawn.Heading, spawn.MapId);
        }

        // Notify players in the old zone that this player is departing
        int oldWorldId = player.Position.WorldId;
        if (oldWorldId != destination.WorldId)
        {
            var deletePacket = new SM_DELETE(player.ObjectId);
            foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == oldWorldId)
                    try { await other.SendAsync(deletePacket, ct); } catch { }
        }

        player.Position = destination;
        await _conn.SendAsync(new SM_TELEPORT_LOC(destination), ct);

        bool crossZone = oldWorldId != destination.WorldId;
        if (crossZone)
        {
            // Cross-zone: new-zone peers have no entity for this player yet — skip emotion broadcast.
            // Send self a zone-load signal after the animation (~2200ms); CM_LEVEL_READY from the client
            // will then broadcast SM_PLAYER_INFO to new-zone peers once the map finishes loading.
            _ = SchedulePostReviveSpawnAsync(player, ct);
        }
        else
        {
            // Same-zone: peers already have the entity — broadcast resurrection emotions.
            int worldId = destination.WorldId;
            var resurrectEmotion = new SM_EMOTION(player, EmotionType.RESURRECT);
            try { await _conn.SendAsync(resurrectEmotion, ct); } catch { }
            foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == worldId)
                    try { await other.SendAsync(resurrectEmotion, ct); } catch { }

            var standEmotion = new SM_EMOTION(player, EmotionType.STAND);
            try { await _conn.SendAsync(standEmotion, ct); } catch { }
            foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == worldId)
                    try { await other.SendAsync(standEmotion, ct); } catch { }
        }

        var tpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        await _conn.SendAsync(new SM_STATS_INFO(player, tpl, _dataManager.ExpTable), ct);

        // Notify self with "revived at bind point" system message
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.Revived(), ct);

        // Notify group members of the updated HP/MP
        var group = player.Group;
        if (group is not null)
        {
            var hpUpdate = new SM_GROUP_MEMBER_INFO(group.GroupId, player, SM_GROUP_MEMBER_INFO.GroupEvent.Update);
            foreach (var member in group.Members)
            {
                if (member.ObjectId == player.ObjectId) continue;
                var memberConn = _connRegistry.Get(member.ObjectId);
                if (memberConn is not null)
                    try { await memberConn.SendAsync(hpUpdate, ct); } catch { }
            }
        }
    }

    private async Task SchedulePostReviveSpawnAsync(Player player, CancellationToken ct)
    {
        try
        {
            await Task.Delay(2200, ct);
            await _conn.SendAsync(new SM_CHANNEL_INFO(), ct);
            await _conn.SendAsync(new SM_PLAYER_SPAWN(player), ct);
        }
        catch (OperationCanceledException) { }
    }
}
