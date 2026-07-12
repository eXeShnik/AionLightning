// Port of Java data/scripts/system/handlers/quest/theobomos/_3086SearchingForTheCrater.java.
// Talk to Metatron (798132) to start; interacting with the crater object (700418) flips to
// REWARD as long as the player doesn't already hold a Crater Fragment (182208062); turn in at
// Ariel (798201).
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

namespace Quest.Theobomos;

public sealed class _3086SearchingForTheCrater : QuestHandlerBase
{
    private const int QuestIdConst  = 3086;
    private const int MetatronNpc   = 798132;
    private const int CraterObject  = 700418;
    private const int ArielNpc      = 798201;
    private const int FragmentItemId = 182208062;

    public _3086SearchingForTheCrater(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MetatronNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MetatronNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CraterObject).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ArielNpc).OnTalk.Add(QuestId);
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
            if (targetId != MetatronNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == CraterObject && dialog == DialogAction.USE_OBJECT)
        {
            long count = player.Inventory.FindByItemId(FragmentItemId)?.Count ?? 0;
            if (count >= 1) return false;

            entry.SetVar(0, entry.GetVar(0) + 1);
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ArielNpc)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
