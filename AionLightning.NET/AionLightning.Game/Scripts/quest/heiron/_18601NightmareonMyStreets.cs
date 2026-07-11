// Port of Java data/scripts/system/handlers/quest/heiron/_18601NightmareonMyStreets.java.
// Talk to Rukiel (204500) to start (receives a note item); turn it back in to Kyrie (205229).
// Skip vs Java: qs.canRepeat() (the daily-repeat/cooldown mechanic) isn't ported in this port, so
// only a first-time run is offered — the quest is still fully completable end-to-end once.
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

namespace Quest.Heiron;

public sealed class _18601NightmareonMyStreets : QuestHandlerBase
{
    private const int QuestIdConst = 18601;
    private const int StartNpc     = 204500;
    private const int EndNpc       = 205229;
    private const int NoteItemId   = 182213002;

    private readonly IItemDao _itemDao;

    public _18601NightmareonMyStreets(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);

            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                if (await StartMissionAsync(conn, player, QuestStatus.START, ct))
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, NoteItemId, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                }
                return false;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != EndNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 0, reward: true, sameNpc: true,
                    giveItemId: 0, giveItemCount: 0, removeItemId: NoteItemId, removeItemCount: 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == EndNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
