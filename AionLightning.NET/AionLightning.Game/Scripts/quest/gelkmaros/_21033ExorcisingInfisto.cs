// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21033ExorcisingInfisto.java (VladimirZ).
// Talk to 799256 (gives Odella-style item 182207829 on accept); relay at 204734 (SETPRO1: give
// 182207830, remove 182207829, var 0->1, reward); turn in at 799256 (dialog 2375).
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

namespace Quest.Gelkmaros;

public sealed class _21033ExorcisingInfisto : QuestHandlerBase
{
    private const int QuestIdConst = 21033;
    private const int StartNpc     = 799256;
    private const int RelayNpc     = 204734;
    private const int StartItemId  = 182207829;
    private const int RelayItemId  = 182207830;

    private readonly IItemDao _itemDao;

    public _21033ExorcisingInfisto(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == RelayNpc)
        {
            int var = entry.GetVar(0);
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: true, sameNpc: false,
                        giveItemId: RelayItemId, giveItemCount: 1, removeItemId: StartItemId, removeItemCount: 1, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
