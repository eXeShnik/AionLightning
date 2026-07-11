using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest;
using AionLightning.Game.Model.Templates.Quest.Script;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.QuestEngine.Handlers.Templates;

/// <summary>
/// Data-driven "collect N items, turn them in" quest handler (Java
/// <c>questEngine.handlers.template.ItemCollecting</c> port) — covers ~1,961 quests from
/// quest_script_data's &lt;item_collecting&gt; entries. No hand-written script needed.
/// </summary>
public sealed class ItemCollectingHandler : QuestHandlerBase
{
    private readonly HashSet<int> _startNpcs;
    private readonly HashSet<int> _endNpcs;
    private readonly int          _startDialogPage;

    public ItemCollectingHandler(ItemCollectingScriptEntry data, IDataManager dataManager,
        IQuestDao questDao, QuestRewardService rewardService)
        : base(data.Id, dataManager, questDao, rewardService)
    {
        _startNpcs       = data.StartNpcIds;
        _endNpcs         = data.EndNpcIds;
        _startDialogPage = data.StartDialogId != 0 ? data.StartDialogId : 4; // DialogPage.ASK_QUEST_ACCEPT_WINDOW
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int npcId in _startNpcs)
        {
            var npc = engine.RegisterQuestNpc(npcId);
            npc.OnQuestStart.Add(QuestId);
            npc.OnTalk.Add(QuestId);
        }

        foreach (int npcId in _endNpcs)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var template = Template;
        if (template is null) return false;

        var player       = env.Player;
        int targetId      = env.TargetId;
        int targetObjId   = env.Target?.ObjectId ?? 0;
        var entry         = player.Quests.Get(QuestId);
        var status        = entry?.Status ?? QuestStatus.NONE;
        var dialog        = DialogActionLookup.FromId(env.DialogId);

        switch (status)
        {
            case QuestStatus.NONE:
                if (!_startNpcs.Contains(targetId)) return false;
                if (player.Level < template.MinLevel) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId, _startDialogPage, ct),
                    DialogAction.QUEST_ACCEPT or DialogAction.QUEST_ACCEPT_1
                        or DialogAction.QUEST_REFUSE or DialogAction.QUEST_REFUSE_1
                        or DialogAction.QUEST_REFUSE_2 or DialogAction.QUEST_REFUSE_SIMPLE
                        => await SendQuestStartDialogAsync(env, conn, ct),
                    _ => false,
                };

            case QuestStatus.START:
                if (!_endNpcs.Contains(targetId)) return false;
                if (dialog != DialogAction.QUEST_SELECT) return false;

                return await TryAdvanceToRewardAsync(conn, player, entry!, template, targetObjId, ct);

            case QuestStatus.REWARD:
                if (!_endNpcs.Contains(targetId)) return false;

                return dialog switch
                {
                    DialogAction.QUEST_SELECT       => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                    DialogAction.SELECT_QUEST_REWARD => await SendQuestEndDialogAsync(env, conn, ct),
                    _ => false,
                };

            default:
                return false;
        }
    }

    /// <summary>
    /// Re-checks collect_items requirements (shared with the legacy QuestService/CM_DIALOG_SELECT
    /// path via <see cref="QuestService.IsRewardReady"/>) and transitions to REWARD when satisfied.
    /// </summary>
    private async ValueTask<bool> TryAdvanceToRewardAsync(GsClientConnection conn, Player player,
        QuestEntry entry, QuestTemplate template, int targetObjId, CancellationToken ct)
    {
        if (!QuestService.IsRewardReady(entry, template, player))
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
    }
}
