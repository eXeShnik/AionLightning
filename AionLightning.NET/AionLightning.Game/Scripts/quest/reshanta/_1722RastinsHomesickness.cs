// Port of Java data/scripts/system/handlers/quest/reshanta/_1722RastinsHomesickness.java (Rhys2002).
// Accept at 278547, walk a fixed 8-npc chain (278560->278517->278544->278532->278539->278524->
// 278555->278567, var 0->7), receive item 182202101 on the final turn-in, then report back to
// 278547 (which removes that item and completes the quest).
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

namespace Quest.Reshanta;

public sealed class _1722RastinsHomesickness : QuestHandlerBase
{
    private const int QuestIdConst = 1722;
    private const int StartNpc     = 278547;
    private const int Npc1         = 278560;
    private const int Npc2         = 278517;
    private const int Npc3         = 278544;
    private const int Npc4         = 278532;
    private const int Npc5         = 278539;
    private const int Npc6         = 278524;
    private const int Npc7         = 278555;
    private const int Npc8         = 278567;
    private const int TurnInItem   = 182202101;

    private readonly IItemDao _itemDao;

    public _1722RastinsHomesickness(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        foreach (int npc in new[] { StartNpc, Npc1, Npc2, Npc3, Npc4, Npc5, Npc6, Npc7, Npc8 })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc && (entry is null || entry.Status == QuestStatus.NONE))
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }
        if (entry is null) return false;

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            await RemoveQuestItemAsync(player, conn, _itemDao, TurnInItem, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        switch (targetId)
        {
            case Npc1:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO1 when var == 0:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            case Npc2:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO2 when var == 1:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    default:
                        return false;
                }
            case Npc3:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SELECT_QUEST_REWARD when var == 2:
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    default:
                        return false;
                }
            case Npc4:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 3:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.SETPRO4 when var == 3:
                        return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                    default:
                        return false;
                }
            case Npc5:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 4:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.SETPRO5 when var == 4:
                        return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                    default:
                        return false;
                }
            case Npc6:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 5:
                        return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    case DialogAction.SETPRO6 when var == 5:
                        return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                    default:
                        return false;
                }
            case Npc7:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 6:
                        return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    case DialogAction.SETPRO7 when var == 6:
                        return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                    default:
                        return false;
                }
            case Npc8:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 7:
                        return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                    case DialogAction.SET_SUCCEED when var == 7:
                        return await DefaultCloseDialogAsync(env, conn, _itemDao, 7, 7, reward: true, sameNpc: false,
                            giveItemId: TurnInItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                    default:
                        return false;
                }
            default:
                return false;
        }
    }
}
