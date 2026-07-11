using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven "craft to learn a recipe skill/level" quest handler (Java
/// <c>questEngine.handlers.template.CraftingRewards</c> port) — covers &lt;crafting_rewards&gt;
/// entries (28 on disk: Expert/Master crafting-skill unlock quests).
/// </summary>
/// <remarks>
/// Java gates the actual skill grant behind <c>onMovieEndEvent</c> (a client cutscene-finished
/// callback) — every one of the 28 entries declares a non-zero <c>movie</c> attribute, so in real
/// Java play the skill is granted only after the movie finishes. This port has no SM_MOVIE/
/// movie-end callback plumbing (same documented gap as <c>ItemCollectingScriptEntry.Movie</c> and
/// <c>MonsterHuntScriptEntry</c>'s unused movie-adjacent fields), so the skill/level is granted
/// unconditionally on the <c>SELECT_QUEST_REWARD</c> click instead — otherwise the quest would be
/// unfinishable. Also not ported: <c>CraftSkillUpdateService.canLearnMoreExpertCraftingSkill</c>/
/// <c>canLearnMoreMasterCraftingSkill</c> (max 2 expert / 1 master crafting skills cap) — a
/// separate crafting-tier subsystem this quest normally re-checks; skipped as out of this
/// template's scope, with only a light "don't regrant an already-learned level" guard kept.
/// </remarks>
public sealed class CraftingRewardsHandler : QuestHandlerBase
{
    private readonly int _startNpc;
    private readonly int _endNpc;
    private readonly int _skillId;
    private readonly int _levelReward;
    private readonly ISkillDao _skillDao;

    public CraftingRewardsHandler(CraftingRewardsScriptEntry data, IDataManager dataManager,
        IQuestDao questDao, QuestRewardService rewardService, ISkillDao skillDao)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpc    = data.StartNpcId;
        _endNpc      = data.EndNpcId != 0 ? data.EndNpcId : data.StartNpcId;
        _skillId     = data.SkillId;
        _levelReward = data.LevelReward;
        _skillDao    = skillDao;
    }

    public override void Register(QuestEngine engine)
    {
        var startNpc = engine.RegisterQuestNpc(_startNpc);
        startNpc.OnQuestStart.Add(QuestId);
        startNpc.OnTalk.Add(QuestId);

        if (_endNpc != _startNpc)
            engine.RegisterQuestNpc(_endNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var template = Template;
        if (template is null) return false;

        var player      = env.Player;
        int targetId     = env.TargetId;
        int targetObjId  = env.Target?.ObjectId ?? 0;
        var entry        = player.Quests.Get(QuestId);
        var status       = entry?.Status ?? QuestStatus.NONE;
        var dialog       = DialogActionLookup.FromId(env.DialogId);

        switch (status)
        {
            case QuestStatus.NONE:
                if (targetId != _startNpc) return false;
                if (player.Level < template.MinLevel) return false;
                if (player.Skills.GetLevel(_skillId) >= _levelReward) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                    DialogAction.QUEST_ACCEPT or DialogAction.QUEST_ACCEPT_1
                        or DialogAction.QUEST_REFUSE or DialogAction.QUEST_REFUSE_1
                        or DialogAction.QUEST_REFUSE_2 or DialogAction.QUEST_REFUSE_SIMPLE
                        => await SendQuestStartDialogAsync(env, conn, ct),
                    _ => false,
                };

            case QuestStatus.START:
                if (targetId != _endNpc) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT        => await SendQuestDialogAsync(conn, targetObjId, 2375, ct),
                    DialogAction.SELECT_QUEST_REWARD => await GrantSkillAndCompleteAsync(env, conn, ct),
                    _ => false,
                };

            case QuestStatus.REWARD:
                if (targetId != _endNpc) return false;
                return await SendQuestEndDialogAsync(env, conn, ct);

            default:
                return false;
        }
    }

    /// <summary>Grants the crafting skill/level, then delegates to the shared reward payout (see remarks above).</summary>
    private async ValueTask<bool> GrantSkillAndCompleteAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Skills.AddSkill(_skillId, _levelReward))
        {
            await _skillDao.UpsertAsync(player.ObjectId, _skillId, _levelReward, ct);
            var entry = player.Skills.GetEntry(_skillId);
            if (entry is not null)
                await conn.SendAsync(new SM_SKILL_LIST([entry], isNew: true, msgId: 1330064), ct);
        }

        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
