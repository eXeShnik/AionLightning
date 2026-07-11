// Port of Java data/scripts/system/handlers/quest/beluslan/_2664AnAntidotetotheLepharists.java
// (VladimirZ). Talk to 204777 to start; use the vial object (700324) while carrying the antidote
// vial item (182204489) to advance var 0 four times (0->4), then a fifth use flips to REWARD;
// turn in back at 204777. Java's checks for the start npc and the reward npc are both gated
// through the same 204777 id, reproduced in the same top-to-bottom flow as the original.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Beluslan;

public sealed class _2664AnAntidotetotheLepharists : QuestHandlerBase
{
    private const int QuestIdConst      = 2664;
    private const int StartNpc          = 204777;
    private const int VialObj           = 700324;
    private const int AntidoteVialItem  = 182204489;

    public _2664AnAntidotetotheLepharists(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VialObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        Player player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc && entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);
        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status != QuestStatus.START)
        {
            return false;
        }

        if (targetId == VialObj && dialog == DialogAction.USE_OBJECT)
        {
            if (var is >= 0 and < 4)
            {
                if ((player.Inventory.FindByItemId(AntidoteVialItem)?.Count ?? 0) < 1)
                    return false;
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            if (var == 4)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
        }

        return false;
    }
}
