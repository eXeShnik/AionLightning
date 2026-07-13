// Port of Java data/scripts/system/handlers/quest/haramel/_18510MurderMyShugo.java.
// Aether Cart kill-quest: accept from Zephyros (203166), who hands over a Cart Key
// (182212009, var 0..3 via kills); kill 3 Aether Carts (700950, vars 0->3), then use the
// Odium-Filled Barrel (700953) two more times (vars 3->5) before turning in back at Zephyros.
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

namespace Quest.Haramel;

public sealed class _18510MurderMyShugo : QuestHandlerBase
{
    private const int QuestIdConst  = 18510;
    private const int ZephyrosNpc   = 203166;
    private const int BarrelObj     = 700953;
    private const int AetherCartNpc = 700950;
    private const int CartKeyItemId = 182212009;

    private readonly IItemDao _itemDao;

    public _18510MurderMyShugo(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ZephyrosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ZephyrosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BarrelObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AetherCartNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, AetherCartNpc, 0, 3, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == ZephyrosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);

                // Java: sendQuestStartDialog(env, 182212009, 1) — gives the Cart Key on QUEST_ACCEPT_1.
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, CartKeyItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == BarrelObj)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.USE_OBJECT)
            {
                if (var >= 3 && var < 5)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
                    return true;
                }
                if (var == 5)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: true, ct);
                    return true;
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ZephyrosNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
