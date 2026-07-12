// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41537StonyHeartedAncestors.java (Cheatkiller).
// Talk to 205916 to start; talk to 205954 to receive the tool (var 0->1); interact with 701147
// three times (var 1->2->3, then reward flip); return to 205954 to turn in.
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

namespace Quest.Tiamaranta;

public sealed class _41537StonyHeartedAncestors : QuestHandlerBase
{
    private const int QuestIdConst = 41537;
    private const int StartNpc     = 205916;
    private const int ToolNpc      = 205954;
    private const int RelicObj     = 701147;
    private const int ToolItemId   = 182212537;

    private readonly IItemDao _itemDao;

    public _41537StonyHeartedAncestors(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ToolNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelicObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == ToolNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, ToolItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == RelicObj)
            {
                if (var > 0 && var < 3)
                    return await UseQuestObjectAsync(env, conn, var, var + 1, reward: false, dieObject: true, ct);
                if (var == 3)
                    return await UseQuestObjectAsync(env, conn, 3, 3, reward: true, dieObject: true, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ToolNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
