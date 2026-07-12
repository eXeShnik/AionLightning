// Port of Java data/scripts/system/handlers/quest/silentera_canyon/_30055ForMyWife.java (Ritsu).
// Preceded by quest 30054. Talk to Gellius (798929) to start; at Telemachus (203901) SETPRO1 gives
// item 182209222 (x1) and advances var 0 to 1; back at Gellius, QUEST_SELECT at var 1 shows dialog
// 2375 and SELECT_QUEST_REWARD removes that item, flips to REWARD, and finishes on the same npc.
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

public sealed class _30055ForMyWife : QuestHandlerBase
{
    private const int QuestIdConst  = 30055;
    private const int GelliusNpc    = 798929;
    private const int TelemachusNpc = 203901;
    private const int LetterItem    = 182209222;

    private readonly IItemDao _itemDao;

    public _30055ForMyWife(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GelliusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GelliusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TelemachusNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry       = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != GelliusNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != GelliusNpc) return false;
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == TelemachusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: LetterItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            }
            else if (targetId == GelliusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD && var == 1)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: true, sameNpc: true,
                        giveItemId: 0, giveItemCount: 0, removeItemId: LetterItem, removeItemCount: 1, ct);
            }
        }

        return false;
    }
}
