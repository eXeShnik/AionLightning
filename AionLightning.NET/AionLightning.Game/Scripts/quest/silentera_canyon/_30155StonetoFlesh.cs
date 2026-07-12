// Port of Java data/scripts/system/handlers/quest/silentera_canyon/_30155StonetoFlesh.java (Ritsu).
// Preceded by quest 30154. Start at Nep (799234); Vili (204433) SETPRO1 gives item 182209252 (x1)
// and advances var 0 to 1; Nep SETPRO2 (var 1) advances to 2; Vili SETPRO3 (var 1) advances to 3 -
// either destination then removes the item and flips to REWARD via its own SELECT_QUEST_REWARD
// dialog. Once in REWARD, turning in at Vili (var 3) finishes normally; turning in at Nep via the
// SELECTED_QUEST_NOREWARD dialog (var 2, i.e. the player stopped at Nep's branch) finishes with
// reward index (var - 2), which is always 0 on this path.
// Java quirks carried over faithfully (missing `break` after several `switch` cases in the
// original, a pervasive pattern across these hand-written scripts): at Nep, simply reopening the
// dialog (QUEST_SELECT) once var reaches 2 falls through into the same item-removal/REWARD-flip
// that SELECT_QUEST_REWARD triggers; at Vili, QUEST_SELECT falls through into SETPRO3's
// unconditional "show dialog 2375" once var != 1; and Vili's own SELECT_QUEST_REWARD branch (var
// 3) has no explicit return in Java, so the state change happens but the dialog call reports
// unhandled (false) - preserved here.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.SilenteraCanyon;

public sealed class _30155StonetoFlesh : QuestHandlerBase
{
    private const int QuestIdConst = 30155;
    private const int NepNpc       = 799234;
    private const int ViliInnNpc   = 204433;
    private const int ViliNpc      = 204304;
    private const int CollectItem  = 182209252;

    private readonly IItemDao _itemDao;

    public _30155StonetoFlesh(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NepNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NepNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ViliInnNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ViliNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != NepNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int var = entry.GetVar(0);

            if (targetId == ViliNpc && var == 3)
                return await SendQuestEndDialogAsync(env, conn, ct);

            if (targetId == NepNpc && dialog == DialogAction.SELECTED_QUEST_NOREWARD && var == 2)
            {
                await FinishQuestAsync(conn, player, var - 2, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }

            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == NepNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);

                if (dialog == DialogAction.SETPRO2 && var == 1)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }

                if ((dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_QUEST_REWARD) && var == 2)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, CollectItem, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                }
            }
            else if (targetId == ViliInnNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: CollectItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            }
            else if (targetId == ViliNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);

                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO3)
                {
                    if (var == 1)
                        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }

                if (dialog == DialogAction.SELECT_QUEST_REWARD && var == 3)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, CollectItem, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
                    return false;
                }
            }
        }

        return false;
    }
}
