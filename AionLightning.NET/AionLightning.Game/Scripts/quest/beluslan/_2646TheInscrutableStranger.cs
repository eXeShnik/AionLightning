// Port of Java data/scripts/system/handlers/quest/beluslan/_2646TheInscrutableStranger.java.
// Talk to 204817 to start; 204777 (var0->1, gives 182204515 and 182204516), 204700 (var1->2,
// removes 182204515), 204702 (var2->3, removes 182204516); talking to 204817 again at var3 flips
// to REWARD.
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

namespace Quest.Beluslan;

public sealed class _2646TheInscrutableStranger : QuestHandlerBase
{
    private const int QuestIdConst = 2646;
    private const int StartNpc  = 204817;
    private const int Relay1Npc = 204777;
    private const int Relay2Npc = 204700;
    private const int Relay3Npc = 204702;
    private const int FirstItem = 182204515;
    private const int SecondItem = 182204516;

    private readonly IItemDao _itemDao;

    public _2646TheInscrutableStranger(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay1Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay2Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay3Npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc && (entry is null || entry.Status == QuestStatus.NONE))
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD && var == 3) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return false;
            }
            if (targetId == Relay1Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, FirstItem, 1, ct)) return true;
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, SecondItem, 1, ct)) return true;
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == Relay2Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: FirstItem, removeItemCount: 1, ct);
                return false;
            }
            if (targetId == Relay3Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 3, reward: false, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: SecondItem, removeItemCount: 1, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
