using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

public sealed class ExperienceService
{
    private readonly IDataManager _dataManager;
    private readonly ILogger<ExperienceService> _log;

    public ExperienceService(IDataManager dataManager, ILogger<ExperienceService> log)
    {
        _dataManager = dataManager;
        _log         = log;
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
            player.MaxHp     = tpl.MaxHp;
            player.MaxMp     = tpl.MaxMp;
            player.CurrentHp = tpl.MaxHp;
            player.CurrentMp = tpl.MaxMp;
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
    }
}
