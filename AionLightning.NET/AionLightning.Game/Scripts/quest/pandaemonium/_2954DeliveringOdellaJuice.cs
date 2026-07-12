// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2954DeliveringOdellaJuice.java.
// Accept at Doman (204191) gives item 182207040 x1; single-dialog step at Haven (204221, var
// 0->1, no reward yet); back at Doman, SELECT_QUEST_REWARD flips to REWARD (Java also writes var
// slot 1 = 1 here — an extra write nothing else reads, ported as-is, harmless); turn in at Doman.
// Skip vs Java (documented): qs.canRepeat() — no repeatable-quest modeling yet, treated as a
// one-time quest (same simplification as ishalgen/_2106VanarsFlattery.cs).
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

namespace Quest.Pandaemonium;

public sealed class _2954DeliveringOdellaJuice : QuestHandlerBase
{
    private const int QuestIdConst = 2954;
    private const int DomanNpc = 204191;
    private const int HavenNpc = 204221;
    private const int GiftItemId = 182207040;

    private readonly IItemDao _itemDao;

    public _2954DeliveringOdellaJuice(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(DomanNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(DomanNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HavenNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != DomanNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (!await GiveQuestItemAsync(player, conn, _itemDao, GiftItemId, 1, ct)) return false;
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == HavenNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == DomanNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == DomanNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
