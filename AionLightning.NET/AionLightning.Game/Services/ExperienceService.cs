using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using AionLightning.Game;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

public sealed class ExperienceService
{
    private readonly IDataManager _dataManager;
    private readonly ILogger<ExperienceService> _log;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GameWorld _world;

    public ExperienceService(IDataManager dataManager, ILogger<ExperienceService> log,
        PlayerConnectionRegistry connRegistry, GameWorld world)
    {
        _dataManager  = dataManager;
        _log          = log;
        _connRegistry = connRegistry;
        _world        = world;
    }

    /// <summary>
    /// Awards XP to a player and their group members in the same world.
    /// XP is split equally among eligible group members; solo kill receives the full amount.
    /// </summary>
    private const float MaxGroupXpRange = 1500f;

    public async ValueTask AddGroupExpAsync(Player killer, long xpPool, CancellationToken ct)
    {
        var group = killer.Group;
        if (group is null)
        {
            var conn = _connRegistry.Get(killer.ObjectId);
            if (conn is not null)
                await AddExpAsync(killer, xpPool, conn, ct);
            return;
        }

        var eligible = group.Members
            .Where(m => !m.IsAlreadyDead
                     && m.Position.WorldId == killer.Position.WorldId
                     && (m.ObjectId == killer.ObjectId
                         || killer.Position.DistanceTo(m.Position) <= MaxGroupXpRange))
            .ToList();

        if (eligible.Count == 0) return;

        long xpPerMember = Math.Max(1, xpPool / eligible.Count);
        foreach (var member in eligible)
        {
            var memberConn = _connRegistry.Get(member.ObjectId);
            if (memberConn is not null)
                await AddExpAsync(member, xpPerMember, memberConn, ct);
        }
    }

    public async ValueTask AddExpAsync(Player player, long amount, GsClientConnection conn, CancellationToken ct)
    {
        if (amount <= 0) return;

        var expTable = _dataManager.ExpTable;
        int maxLevel = expTable.MaxLevel;
        long cap     = expTable.GetStartExpForLevel(maxLevel);

        long newExp = Math.Min(player.Exp + amount, cap);
        int  oldLvl = player.Level;

        player.Exp   = newExp;
        player.Level = (byte)expTable.GetLevelForExp(newExp);

        if (player.Level != oldLvl)
            await HandleLevelUpAsync(player, conn, ct);

        long expNeeded = expTable.GetStartExpForLevel(player.Level + 1);
        await conn.SendAsync(new SM_STATUPDATE_EXP(player.Exp, 0, expNeeded), ct);
    }

    private async ValueTask HandleLevelUpAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        _log.LogInformation("Player {Name} leveled up to {Level}", player.Name, player.Level);

        var tpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        if (tpl is not null)
        {
            player.MaxHp              = tpl.MaxHp + player.BonusMaxHp;
            player.MaxMp              = tpl.MaxMp + player.BonusMaxMp;
            player.CurrentHp          = player.MaxHp;
            player.CurrentMp          = player.MaxMp;
            player.BasePhysicalAttack = tpl.MainHandAttack;
        }

        // Auto-learn new skills for the new level
        foreach (var slt in _dataManager.SkillTree.GetTemplatesFor(player.PlayerClass, player.Level, player.Race))
        {
            if (slt.AutoLearn)
                player.Skills.AddSkill(slt.SkillId, slt.SkillLevel, slt.Stigma);
        }

        await conn.SendAsync(new SM_LEVEL_UPDATE(player.ObjectId, 0, player.Level), ct);
        await conn.SendAsync(new SM_STATS_INFO(player, tpl, _dataManager.ExpTable), ct);
        await conn.SendAsync(new SM_SKILL_LIST(player.Skills.AllSkills, isNew: true), ct);

        var levelUpdate = new SM_LEVEL_UPDATE(player.ObjectId, 0, player.Level);
        foreach (var other in _world.GetAll())
        {
            if (other.ObjectId == player.ObjectId) continue;
            if (other.Position.WorldId != player.Position.WorldId) continue;
            var otherConn = _connRegistry.Get(other.ObjectId);
            if (otherConn is not null)
            {
                try { await otherConn.SendAsync(levelUpdate, ct); }
                catch { /* ignore disconnected peers */ }
            }
        }
    }
}
