// Port of Java data/scripts/system/handlers/quest/eltnen/_1414OperationWindmill.java (Xitanium).
// Talk to Tumblusen (203989) to start (gives item 182201349 on accept); use the Old Gear
// (700175) to consume it and flip straight to REWARD; return to Tumblusen to finish.
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

namespace Quest.Eltnen;

public sealed class _1414OperationWindmill : QuestHandlerBase
{
    private const int QuestIdConst = 1414;
    private const int TumblusenNpc = 203989;
    private const int OldGearObj   = 700175;
    private const int GearItemId   = 182201349;

    private readonly IItemDao _itemDao;

    public _1414OperationWindmill(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TumblusenNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TumblusenNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OldGearObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == TumblusenNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, GearItemId, 1, ct)) return true;
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        if (entry.Status == QuestStatus.START && entry.GetVar(0) == 0 && targetId == OldGearObj)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                entry.SetVar(0, 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, GearItemId, 1, ct);
                return true;
            }
        }
        return false;
    }
}
