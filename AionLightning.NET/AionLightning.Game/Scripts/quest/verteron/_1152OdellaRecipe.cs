// Port of Java data/scripts/system/handlers/quest/verteron/_1152OdellaRecipe.java (vlog).
// Talk to Nemia (203132), giving the Odella (182200526) on accept; give it to Eradis (203130)
// (SETPRO1, var 0->1); collect-check turns in Verteron Pepper (var 1 -> REWARD).
using System.Linq;
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

namespace Quest.Verteron;

public sealed class _1152OdellaRecipe : QuestHandlerBase
{
    private const int QuestIdConst = 1152;
    private const int NemiaNpc     = 203132;
    private const int EradisNpc    = 203130;
    private const int OdellaItemId = 182200526;

    private readonly IItemDao _itemDao;

    public _1152OdellaRecipe(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NemiaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NemiaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EradisNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null)
        {
            if (targetId == NemiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, OdellaItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == EradisNpc)
        {
            int var = entry.GetVar(0);
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.QUEST_SELECT when var == 1:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: OdellaItemId, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, true, 5, 2716, ct);
                case DialogAction.FINISH_DIALOG:
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == EradisNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
