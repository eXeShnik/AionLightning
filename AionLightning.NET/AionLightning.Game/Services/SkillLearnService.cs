using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Single entry point for teaching a player a skill. Pairs the in-memory
/// <see cref="Model.Skill.PlayerSkillList"/> update with DB persistence (and, optionally,
/// the client <see cref="SM_SKILL_LIST"/> notification) so the three can never drift apart.
/// Previously each of the ~8 learn call sites hand-paired <c>AddSkill</c> + <c>UpsertAsync</c>;
/// any that forgot the pairing dropped the skill silently on the next relog.
/// </summary>
public sealed class SkillLearnService
{
    private readonly ISkillDao    _skillDao;
    private readonly IDataManager _dataManager;
    private readonly ILogger<SkillLearnService> _logger;

    public SkillLearnService(ISkillDao skillDao, IDataManager dataManager, ILogger<SkillLearnService> logger)
    {
        _skillDao    = skillDao;
        _dataManager = dataManager;
        _logger      = logger;
    }

    /// <summary>
    /// Learns or upgrades a single skill. When the list actually changes it is persisted
    /// (stigma skills are transient and never written to <c>player_skills</c>), and when
    /// <paramref name="notify"/> is set with a live connection a single-entry SM_SKILL_LIST
    /// is sent. Returns true when the skill was newly learned or upgraded.
    /// </summary>
    public async ValueTask<bool> LearnSkillAsync(Player player, int skillId, int skillLevel,
        bool isStigma = false, GsClientConnection? conn = null, bool notify = false,
        int msgId = 0, string skillName = "", CancellationToken ct = default)
    {
        if (!player.Skills.AddSkill(skillId, skillLevel, isStigma))
            return false;

        if (!isStigma)
            await _skillDao.UpsertAsync(player.ObjectId, skillId, skillLevel, ct);

        if (notify && conn is not null)
        {
            var entry = player.Skills.GetEntry(skillId);
            if (entry is not null)
                await conn.SendAsync(new SM_SKILL_LIST([entry], isNew: true,
                    msgId: msgId, skillName: skillName, skillLevel: skillLevel), ct);
        }

        return true;
    }

    /// <summary>
    /// Applies every auto-learn skill-tree entry for the player's class/race across the inclusive
    /// level range, in memory only. These are deterministic from the skill tree and re-derived on
    /// every login, so they are intentionally NOT persisted. Returns the number of skills newly
    /// learned or upgraded.
    /// </summary>
    public int ApplyAutoLearn(Player player, int minLevel, int maxLevel)
    {
        int changed = 0;
        for (int lvl = minLevel; lvl <= maxLevel; lvl++)
            foreach (var slt in _dataManager.SkillTree.GetTemplatesFor(player.PlayerClass, lvl, player.Race))
                if (slt.AutoLearn && player.Skills.AddSkill(slt.SkillId, slt.SkillLevel, slt.Stigma))
                    changed++;
        return changed;
    }

    /// <summary>Persists every non-stigma skill the player currently knows (bulk save).</summary>
    public async ValueTask PersistAllAsync(Player player, CancellationToken ct = default)
    {
        foreach (var sk in player.Skills.AllSkills)
        {
            if (sk.IsStigma) continue;
            await _skillDao.UpsertAsync(player.ObjectId, sk.SkillId, sk.SkillLevel, ct);
        }
    }
}
