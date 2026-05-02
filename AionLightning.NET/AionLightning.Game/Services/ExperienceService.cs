using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AionLightning.Game;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

public sealed class ExperienceService
{
    private readonly IDataManager _dataManager;
    private readonly ILogger<ExperienceService> _log;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GameWorld _world;
    private readonly RateOptions _rates;

    public ExperienceService(IDataManager dataManager, ILogger<ExperienceService> log,
        PlayerConnectionRegistry connRegistry, GameWorld world, IOptions<RateOptions> rates)
    {
        _dataManager  = dataManager;
        _log          = log;
        _connRegistry = connRegistry;
        _world        = world;
        _rates        = rates.Value;
    }

    /// <summary>
    /// Awards XP to a player and their group members in the same world.
    /// Mirrors Java PlayerTeamDistributionService.doReward XP logic:
    ///   - Solo: level-diff scaling by killer's level.
    ///   - Group: level-diff scaling by highest eligible member's level;
    ///     proportional per-member share (member.Level / sum of member levels);
    ///     group bonus of 150% + 10% per extra member beyond 2;
    ///     members 10+ levels below the highest receive 0 XP.
    /// </summary>
    private const float MaxGroupXpRange = 1500f;

    public async ValueTask AddGroupExpAsync(Player killer, long xpBase, int npcLevel, CancellationToken ct)
    {
        var group = killer.Group;
        if (group is null)
        {
            int pct  = XpRewardPercent(npcLevel - killer.Level);
            long xp  = (long)(xpBase * pct / 100L * _rates.XpRate);
            var conn = _connRegistry.Get(killer.ObjectId);
            if (conn is not null && xp > 0)
                await AddExpAsync(killer, xp, conn, ct);
            return;
        }

        var eligible = group.Members
            .Where(m => !m.IsAlreadyDead
                     && m.Position.WorldId == killer.Position.WorldId
                     && (m.ObjectId == killer.ObjectId
                         || killer.Position.DistanceTo(m.Position) <= MaxGroupXpRange))
            .ToList();

        if (eligible.Count == 0) return;

        int highestLevel = eligible.Max(m => (int)m.Level);
        int xpPct  = XpRewardPercent(npcLevel - highestLevel);
        long xpPool = xpBase * xpPct / 100;
        if (xpPool <= 0) return;

        // Group bonus: solo=100%, 2=150%, 3=160%, 4=170%, 5=180%, 6=190%
        int bonus = eligible.Count > 1 ? 150 + (eligible.Count - 2) * 10 : 100;
        long partyLvlSum = eligible.Sum(m => (long)m.Level);
        if (partyLvlSum <= 0) return;

        foreach (var member in eligible)
        {
            if (highestLevel - member.Level >= 10) continue; // too low — no reward
            long memberXp = (long)(xpPool * bonus * member.Level / (partyLvlSum * 100L) * _rates.XpRate);
            if (memberXp <= 0) continue;
            var memberConn = _connRegistry.Get(member.ObjectId);
            if (memberConn is not null)
                await AddExpAsync(member, memberXp, memberConn, ct);
        }
    }

    public async ValueTask AddQuestExpAsync(Player player, long amount, GsClientConnection conn, CancellationToken ct)
        => await AddExpAsync(player, (long)(amount * _rates.QuestXpRate), conn, ct);

    /// <summary>
    /// Awards gathering XP using the Java GatherableController formula:
    /// base = 0.0031 * (skillLvl + 5.3) * (skillLvl + 1592.8) + 60, scaled by GatheringXpRate.
    /// </summary>
    public async ValueTask AddGatheringExpAsync(Player player, int skillLevel, GsClientConnection conn, CancellationToken ct)
    {
        long base_ = (long)(0.0031 * (skillLevel + 5.3) * (skillLevel + 1592.8) + 60);
        long xp    = _rates.GatheringXpRate != 1.0f ? Math.Max(1, (long)(base_ * _rates.GatheringXpRate)) : base_;
        await AddExpAsync(player, xp, conn, ct);
    }

    /// <summary>
    /// Awards crafting XP using the Java CraftService formula:
    /// base = 0.008 * (skillPoint + 100)^2 + 60, scaled by CraftingXpRate.
    /// </summary>
    public async ValueTask AddCraftingExpAsync(Player player, int skillPoint, GsClientConnection conn, CancellationToken ct)
    {
        long sp    = skillPoint + 100;
        long base_ = (long)(0.008 * sp * sp + 60);
        long xp    = _rates.CraftingXpRate != 1.0f ? Math.Max(1, (long)(base_ * _rates.CraftingXpRate)) : base_;
        await AddExpAsync(player, xp, conn, ct);
    }

    /// <summary>
    /// Applies death XP loss to the player and returns the amount lost.
    /// Mirrors Java XPLossEnum + PlayerCommonData.calculateExpLoss():
    ///   loss = 0 for level &lt; 50; 0.25% of (nextLevelXP - currentXP) for level 50+.
    ///   33% of loss is unrecoverable; 67% is added to ExpRecoverable.
    ///   ExpRecoverable is capped at 25% of expNeeded.
    ///   ExpRecoverable drains as the player re-earns XP (handled in AddExpAsync).
    /// </summary>
    public long ApplyDeathXpLoss(Player player, IDataManager dataManager)
    {
        if (player.Level < 50) return 0;

        var expTable    = dataManager.ExpTable;
        long nextLevel  = expTable.GetStartExpForLevel(player.Level + 1);
        long expNeed    = nextLevel - player.Exp;
        if (expNeed <= 0) return 0;

        long expLost        = (long)(expNeed * 0.0025);
        if (expLost <= 0) return 0;

        long unrecoverable  = expLost / 3;                    // ≈ 33%
        long recoverable    = expLost - unrecoverable;        // ≈ 67%
        long cap            = (long)(expNeed * 0.25);

        player.Exp           = Math.Max(expTable.GetStartExpForLevel(player.Level), player.Exp - expLost);
        player.ExpRecoverable = Math.Min(cap, player.ExpRecoverable + recoverable);

        return expLost;
    }

    /// <summary>Drains ExpRecoverable as the player earns XP back (mirrors Java addExp → expRecoverable drain).</summary>
    private static void DrainRecoverable(Player player, long xpGained)
    {
        if (player.ExpRecoverable <= 0 || xpGained <= 0) return;
        player.ExpRecoverable = Math.Max(0, player.ExpRecoverable - xpGained);
    }

    /// <summary>XP reward percentage for level diff (npcLevel - playerLevel). Matches Java XPRewardEnum.</summary>
    public static int XpRewardPercent(int levelDiff) => levelDiff switch
    {
        <= -11 => 0,
        -10    => 1,
        -9     => 10,
        -8     => 20,
        -7     => 30,
        -6     => 40,
        -5     => 50,
        -4     => 70,
        -3     => 90,
        <= 0   => 100,
        1      => 105,
        2      => 110,
        3      => 115,
        _      => 120,
    };

    public async ValueTask AddExpAsync(Player player, long amount, GsClientConnection conn, CancellationToken ct)
    {
        if (amount <= 0) return;

        var expTable = _dataManager.ExpTable;
        int maxLevel = expTable.MaxLevel;
        long cap     = expTable.GetStartExpForLevel(maxLevel);

        DrainRecoverable(player, amount);

        long newExp = Math.Min(player.Exp + amount, cap);
        int  oldLvl = player.Level;

        player.Exp   = newExp;
        player.Level = (byte)expTable.GetLevelForExp(newExp);

        if (player.Level != oldLvl)
            await HandleLevelUpAsync(player, conn, ct);

        long expNeeded = expTable.GetStartExpForLevel(player.Level + 1);
        await conn.SendAsync(new SM_STATUPDATE_EXP(player.Exp, player.ExpRecoverable, expNeeded), ct);
    }

    private async ValueTask HandleLevelUpAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        _log.LogInformation("Player {Name} leveled up to {Level}", player.Name, player.Level);

        var tpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        if (tpl is not null)
        {
            player.MaxHp = (int)((tpl.MaxHp + player.BonusMaxHp + player.PassiveBonusMaxHp + player.TitleBonusMaxHp) * player.SoulSicknessMultiplier);
            player.MaxMp = (int)((tpl.MaxMp + player.BonusMaxMp + player.PassiveBonusMaxMp + player.TitleBonusMaxMp) * player.SoulSicknessMultiplier);
            player.CurrentHp            = player.MaxHp;
            player.CurrentMp            = player.MaxMp;
            player.BasePhysicalAttack   = tpl.MainHandAttack;
            player.BasePhysicalAccuracy = tpl.MainHandAccuracy;
            player.BaseCritRating       = tpl.MainHandCritRate;
            player.BaseEvasion          = tpl.Evasion;
            player.BaseMagicAccuracy    = tpl.MagicAccuracy;
            player.BaseParry            = tpl.Parry;
            player.BaseBlock            = tpl.Block;
            player.MovementSpeed        = tpl.RunSpeed;
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

        // At level 9 with a starting class, show class selection dialog so player can ascend
        if (player.Level == 9 && player.PlayerClass.IsStartingClass())
            await SendClassSelectionDialogAsync(player, conn, ct);

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

    // Dialog IDs from Java ClassChangeService.showClassChangeDialog (ENABLE_SIMPLE_2NDCLASS path)
    private static ValueTask SendClassSelectionDialogAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        // targetObjectId=0 = dialog with no NPC target; questId identifies the ascension quest
        (int dialogId, int questId) = (player.Race, player.PlayerClass) switch
        {
            (Race.ELYOS,     PlayerClass.WARRIOR) => (2375, 1006),
            (Race.ELYOS,     PlayerClass.SCOUT)   => (2716, 1006),
            (Race.ELYOS,     PlayerClass.MAGE)    => (3057, 1006),
            (Race.ELYOS,     PlayerClass.PRIEST)  => (3398, 1006),
            (Race.ASMODIANS, PlayerClass.WARRIOR) => (3057, 2008),
            (Race.ASMODIANS, PlayerClass.SCOUT)   => (3398, 2008),
            (Race.ASMODIANS, PlayerClass.MAGE)    => (3739, 2008),
            (Race.ASMODIANS, PlayerClass.PRIEST)  => (4080, 2008),
            _ => (0, 0),
        };
        if (dialogId == 0) return ValueTask.CompletedTask;
        return conn.SendAsync(new SM_DIALOG_WINDOW(0, dialogId, questId), ct);
    }
}
