using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_REVIVE : AionClientPacket
{
    // ReviveType IDs from Java ReviveType.java
    private const int TypeBind     = 0;
    private const int TypeRebirth  = 1;
    private const int TypeItemSelf = 2;
    private const int TypeSkill    = 3;
    private const int TypeKisk     = 4;
    private const int TypeInstance = 6;
    private const int TypeObelisk  = 8;

    // Self-resurrection stone item IDs (Java Player.getSelfRezStone priority order)
    private static readonly int[] SelfRezStoneIds = [161001001, 161000003, 161000004, 161000001];

    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager             _dataManager;
    private readonly IPlayerDao               _playerDao;
    private readonly IItemDao                 _itemDao;

    private int _reviveId;

    public CM_REVIVE(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        IDataManager dataManager, IPlayerDao playerDao, IItemDao itemDao)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _dataManager  = dataManager;
        _playerDao    = playerDao;
        _itemDao      = itemDao;
    }

    public override void Read(ref PacketReader r) => _reviveId = r.ReadC();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || !player.IsAlreadyDead) return;

        switch (_reviveId)
        {
            case TypeBind:
            case TypeObelisk:
                await HandleBindReviveAsync(player, ct);
                break;
            case TypeSkill:
                await HandleSkillReviveAsync(player, ct);
                break;
            case TypeRebirth:
                await HandleRebirthReviveAsync(player, ct);
                break;
            case TypeItemSelf:
                await HandleItemSelfReviveAsync(player, ct);
                break;
            case TypeInstance:
                await HandleInstanceReviveAsync(player, ct);
                break;
            // TypeKisk requires Kisk entity support — fall through to bind revive
            default:
                await HandleBindReviveAsync(player, ct);
                break;
        }
    }

    private async ValueTask HandleBindReviveAsync(Player player, CancellationToken ct)
    {
        Position destination;
        if (player.BindPosition.HasValue)
            destination = player.BindPosition.Value;
        else
        {
            var spawn = _dataManager.PlayerInitial.GetSpawnLocation(player.Race);
            destination = new Position(spawn.X, spawn.Y, spawn.Z, spawn.Heading, spawn.MapId);
        }

        await ReviveCoreAsync(player, hpPct: 25, mpPct: 25, applySoulSickness: true, skillId: 0,
            destination: destination, ct: ct);
    }

    private async ValueTask HandleSkillReviveAsync(Player player, CancellationToken ct)
    {
        if (!player.HasPendingRevive) return;
        player.HasPendingRevive    = false;
        int skillId                = player.ResurrectionSkillId;
        player.ResurrectionSkillId = 0;

        await ReviveCoreAsync(player, hpPct: 10, mpPct: 10, applySoulSickness: false, skillId: skillId,
            destination: null, ct: ct);
    }

    private async ValueTask HandleRebirthReviveAsync(Player player, CancellationToken ct)
    {
        if (!player.CanRebirthRevive) return;
        int pct    = Math.Max(1, player.RebirthResurrectPercent);
        int skillId = player.RebirthSkillId;
        player.CanRebirthRevive        = false;
        player.RebirthResurrectPercent = 5;
        player.RebirthSkillId          = 0;

        await ReviveCoreAsync(player, hpPct: pct, mpPct: pct, applySoulSickness: true, skillId: skillId,
            destination: null, ct: ct);
    }

    private async ValueTask HandleItemSelfReviveAsync(Player player, CancellationToken ct)
    {
        // Find highest-priority self-rez stone in inventory
        Model.Item.Item? stone = null;
        foreach (int stoneId in SelfRezStoneIds)
        {
            stone = player.Inventory.All.FirstOrDefault(i => i.ItemId == stoneId && !i.IsEquipped);
            if (stone is not null) break;
        }
        if (stone is null) return;

        // Consume one charge
        stone.Count--;
        if (stone.Count <= 0)
        {
            player.Inventory.Remove(stone.UniqueId);
            await _itemDao.DeleteAsync(stone.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(stone.UniqueId), ct);
        }
        else
        {
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([stone]), ct);
        }

        // Broadcast item-use animation
        var anim = new SM_ITEM_USAGE_ANIMATION(player.ObjectId, (int)stone.UniqueId, stone.ItemId);
        int animWorld = player.Position.WorldId;
        foreach (var c in _connRegistry.GetAll())
            if (c.ActivePlayer?.Position.WorldId == animWorld)
                try { await c.SendAsync(anim, ct); } catch { }

        await ReviveCoreAsync(player, hpPct: 15, mpPct: 15, applySoulSickness: true, skillId: 0,
            destination: null, ct: ct);
    }

    private async ValueTask HandleInstanceReviveAsync(Player player, CancellationToken ct)
    {
        Position destination;
        if (player.InstanceStartPosition.HasValue)
            destination = player.InstanceStartPosition.Value;
        else if (player.BindPosition.HasValue)
            destination = player.BindPosition.Value;
        else
        {
            var spawn = _dataManager.PlayerInitial.GetSpawnLocation(player.Race);
            destination = new Position(spawn.X, spawn.Y, spawn.Z, spawn.Heading, spawn.MapId);
        }

        await ReviveCoreAsync(player, hpPct: 25, mpPct: 25, applySoulSickness: true, skillId: 0,
            destination: destination, ct: ct);
    }

    /// <summary>
    /// Core revive: applies soul sickness, restores HP/MP, clears Dead state, drains DP,
    /// optionally teleports, broadcasts emotions and abnormal effects, updates stats.
    /// <paramref name="destination"/> null means revive in-place (skill/rebirth/item self-rez).
    /// </summary>
    private async ValueTask ReviveCoreAsync(Player player, int hpPct, int mpPct,
        bool applySoulSickness, int skillId, Position? destination, CancellationToken ct)
    {
        // M331: noresurrectpenalty buff suppresses soul sickness on death
        if (applySoulSickness && player.GetActiveEffects().Any(e => e.IsNoDeathPenalty))
            applySoulSickness = false;
        if (applySoulSickness && player.SoulSicknessCount < 10)
        {
            player.SoulSicknessCount++;
            await _playerDao.UpdateSoulSicknessAsync(player.ObjectId, player.SoulSicknessCount, ct);
        }

        // Recompute MaxHp/MaxMp with current soul sickness multiplier
        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        float ssMult = player.SoulSicknessMultiplier;
        player.MaxHp = (int)(((statTpl?.MaxHp ?? 1000) + player.BonusMaxHp + player.PassiveBonusMaxHp + player.TitleBonusMaxHp) * ssMult);
        player.MaxMp = (int)(((statTpl?.MaxMp ?? 500)  + player.BonusMaxMp + player.PassiveBonusMaxMp + player.TitleBonusMaxMp) * ssMult);

        if (player.SoulSicknessCount > 0)
            player.AddEffect(new AbnormalState
            {
                SkillId    = 8291,
                SkillLevel = player.SoulSicknessCount,
                EffectorId = player.ObjectId,
                Expiry     = DateTime.MaxValue
            });

        player.CurrentHp = Math.Max(1, player.MaxHp * hpPct / 100);
        player.CurrentMp = Math.Max(1, player.MaxMp * mpPct / 100);
        player.State &= ~CreatureState.Dead;

        if (player.Dp > 0)
        {
            player.Dp = 0;
            await _playerDao.UpdateDpAsync(player.ObjectId, 0, ct);
            await _conn.SendAsync(new SM_DP_INFO(player.ObjectId, 0), ct);
        }

        int oldWorldId = player.Position.WorldId;
        bool crossZone = destination.HasValue && destination.Value.WorldId != oldWorldId;

        if (destination.HasValue)
        {
            if (crossZone)
            {
                var deletePacket = new SM_DELETE(player.ObjectId);
                foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                    if (other.ActivePlayer?.Position.WorldId == oldWorldId)
                        try { await other.SendAsync(deletePacket, ct); } catch { }
            }
            player.Position = destination.Value;
            await _conn.SendAsync(new SM_TELEPORT_LOC(destination.Value), ct);
        }

        int worldId = player.Position.WorldId;

        if (crossZone)
        {
            _ = SchedulePostReviveSpawnAsync(player, ct);
        }
        else
        {
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

            var ssAbnormal = new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true, player.GetActiveEffects());
            try { await _conn.SendAsync(ssAbnormal, ct); } catch { }
            foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == worldId)
                    try { await other.SendAsync(ssAbnormal, ct); } catch { }
        }

        var tpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        await _conn.SendAsync(new SM_STATS_INFO(player, tpl, _dataManager.ExpTable), ct);
        await _conn.SendAsync(SM_SYSTEM_MESSAGE.Revived(), ct);

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
