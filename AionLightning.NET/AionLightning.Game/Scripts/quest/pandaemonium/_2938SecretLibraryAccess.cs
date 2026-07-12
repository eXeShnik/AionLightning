// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2938SecretLibraryAccess.java.
// Accept at 204267; at 203557 the dialog offered depends on whether quest 2022 (Altgard) or 24016
// (Aethertech) is already COMPLETE; SET_SUCCEED gives item 182207026 and flips to REWARD; back at
// 204267 with var0==0, SELECTED_QUEST_NOREWARD removes the item, bumps var0, and completes (Java's
// sendQuestEndDialog(env) accepts the SELECTED_QUEST_REWARDn/NOREWARD range as a completion
// trigger — this port's base SendQuestEndDialogAsync only accepts SELECT_QUEST_REWARD, so this
// specific branch calls FinishQuestAsync directly instead, matching Java's actual behavior); the
// plain SELECT_QUEST_REWARD click also completes it directly via the base helper.
// Java NPE bug fixed: the COMPLETE-status branch at npc 204268 read `qs.getStatus()` without a
// null check (`qs` can be null if the player never started this quest) — a guaranteed
// NullPointerException in the original Java for anyone who talks to that npc without ever
// accepting 2938. This port guards on `entry is not null` first.
// Skip vs Java (documented): both post-COMPLETE teleport-to-Pandaemonium calls (at 204267 and
// 204268) use TeleportService2, which isn't ported — they're cosmetic repeat-click conveniences
// after the quest is already finished, not required for completion, so they're simply omitted.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Pandaemonium;

public sealed class _2938SecretLibraryAccess : QuestHandlerBase
{
    private const int QuestIdConst = 2938;
    private const int StartNpc = 204267;
    private const int StepNpc = 203557;
    private const int TurnInNpc = 204268;
    private const int AccessItem = 182207026;
    private const int AltgardCampaignQuest = 2022;
    private const int AethertechCampaignQuest = 24016;

    private readonly IItemDao _itemDao;

    public _2938SecretLibraryAccess(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECTED_QUEST_NOREWARD)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, AccessItem, 1, ct);
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    return await FinishQuestAsync(conn, player, 0, ct);
                }
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestEndDialogAsync(env, conn, ct);
            }
            // COMPLETE-status teleport skipped (see file header) — no-op.
            return false;
        }

        if (targetId == StepNpc && entry is { Status: QuestStatus.START } && entry.GetVar(0) == 0)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                bool ready = IsQuestComplete(player, AltgardCampaignQuest) || IsQuestComplete(player, AethertechCampaignQuest);
                return await SendQuestDialogAsync(conn, targetObjId, ready ? 1011 : 1097, ct);
            }
            if (env.DialogId == (int)DialogAction.SET_SUCCEED)
            {
                if (await GiveQuestItemAsync(player, conn, _itemDao, AccessItem, 1, ct))
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                }
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 0), ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        // targetId == TurnInNpc: post-COMPLETE teleport skipped (see file header) — no-op.
        return false;
    }

    private static bool IsQuestComplete(Player player, int questId)
    {
        var qs = player.Quests.Get(questId);
        return qs is { Status: QuestStatus.COMPLETE };
    }
}
