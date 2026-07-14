using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Single entry point for the starter-to-main class ascension (Java ClassChangeService.setClass).
/// Validates the switch, mutates and persists the player's class, grants/persists the new class's
/// skills, refreshes HP/MP/attack from the new class's stat template, and sends the client's
/// dialog-close/stats/skill-list refresh packets. Used both by the class-change dialog UI
/// (<see cref="Network.Aion.ClientPackets.CM_DIALOG_SELECT"/>) and by the ascension quest scripts
/// (1006/2008) via <see cref="QuestEngine.Handlers.QuestHandlerBase"/>, so the two entry points can
/// never drift apart.
/// </summary>
public sealed class ClassChangeService
{
    private readonly IPlayerDao _playerDao;
    private readonly SkillLearnService _skillLearn;
    private readonly IDataManager _dataManager;
    private readonly ILogger<ClassChangeService> _logger;

    public ClassChangeService(IPlayerDao playerDao, SkillLearnService skillLearn,
        IDataManager dataManager, ILogger<ClassChangeService> logger)
    {
        _playerDao   = playerDao;
        _skillLearn  = skillLearn;
        _dataManager = dataManager;
        _logger      = logger;
    }

    /// <summary>
    /// Ascends <paramref name="player"/> to <paramref name="newClass"/>. Returns false without any
    /// effect if the switch fails validation (Java ClassChangeService.validateSwitch): player must
    /// currently be a starting class, be at least level 9, and <paramref name="newClass"/> must be
    /// one of that starting class's valid ascension options.
    /// </summary>
    public async ValueTask<bool> ChangeClassAsync(Player player, PlayerClass newClass,
        GsClientConnection conn, CancellationToken ct)
    {
        if (!ValidateSwitch(player, newClass)) return false;

        var oldClass = player.PlayerClass;
        player.PlayerClass = newClass;
        await _playerDao.UpdateClassAsync(player.ObjectId, newClass, ct);

        // Grant all skills for the new class from level 1 to current level (Java addMissingSkills), then persist.
        _skillLearn.ApplyAutoLearn(player, 1, player.Level);
        await _skillLearn.PersistAllAsync(player, ct);

        // Refresh stats for the new class (Java PlayerController.upgradePlayer's stat-template swap).
        var tpl = _dataManager.PlayerStats.GetTemplate(newClass, player.Level);
        if (tpl is not null)
        {
            player.MaxHp     = (int)((tpl.MaxHp + player.BonusMaxHp + player.PassiveBonusMaxHp + player.TitleBonusMaxHp) * player.SoulSicknessMultiplier);
            player.MaxMp     = (int)((tpl.MaxMp + player.BonusMaxMp + player.PassiveBonusMaxMp + player.TitleBonusMaxMp) * player.SoulSicknessMultiplier);
            player.CurrentHp = player.MaxHp;
            player.CurrentMp = player.MaxMp;
            player.BasePhysicalAttack = tpl.MainHandAttack;
        }

        await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0, 0), ct); // close the class selection UI
        await conn.SendAsync(new SM_STATS_INFO(player, tpl, _dataManager.ExpTable), ct);
        await conn.SendAsync(new SM_SKILL_LIST(player.Skills.AllSkills, isNew: true), ct);

        _logger.LogInformation("Player {Name} ascended from {Old} to {New}", player.Name, oldClass, newClass);
        return true;
    }

    private static bool ValidateSwitch(Player player, PlayerClass newClass)
    {
        if (player.Level < 9) return false;
        if (!player.PlayerClass.IsStartingClass()) return false;
        return player.PlayerClass.IsValidAscension(newClass);
    }
}
