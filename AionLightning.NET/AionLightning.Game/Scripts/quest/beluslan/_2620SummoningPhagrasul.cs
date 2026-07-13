// Port of Java data/scripts/system/handlers/quest/beluslan/_2620SummoningPhagrasul.java.
// Talk to Chieftain Akagitan (204787) to start (gives 182204498); talk to Gigantic Phagrasul
// (204824, var0->1); kill up to 5 each of 213109 (var1) and 213111 (var2) while var0==1; using the
// Huge Mamut Skull (700323) while var0==0 consumes the key item after a short delay (if still
// targeted) and spawns 204824; talking to Chieftain again flips straight to REWARD. Skip vs Java:
// the 40s scheduled despawn of 204824 after SETPRO1 (Java npc.getController().onDelete(), no NPC
// despawn control ported) and the SM_USE_OBJECT/SM_EMOTION cosmetic packets are omitted.
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

public sealed class _2620SummoningPhagrasul : QuestHandlerBase
{
    private const int QuestIdConst = 2620;
    private const int ChieftainNpc = 204787;
    private const int PhagrasulNpc = 204824;
    private const int SkullObj = 700323;
    private const int KeyItem = 182204498;
    private const int BeluslanWorldId = 220040000;
    private static readonly int[] _mobIds = [213109, 213111];

    private readonly IItemDao _itemDao;

    public _2620SummoningPhagrasul(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ChieftainNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ChieftainNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PhagrasulNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SkullObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(213109).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(213111).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (env.TargetId == 213109 && entry.GetVar(1) < 5 && entry.GetVar(0) == 1)
        {
            entry.SetVar(1, entry.GetVar(1) + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        if (env.TargetId == 213111 && entry.GetVar(2) < 5 && entry.GetVar(0) == 1)
        {
            entry.SetVar(2, entry.GetVar(2) + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == ChieftainNpc && (entry is null || entry.Status == QuestStatus.NONE))
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, KeyItem, 1, ct)) return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == PhagrasulNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == SkullObj)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 0)
                {
                    int skullObjId = targetObjId;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        await RemoveQuestItemAsync(player, conn, _itemDao, KeyItem, 1, CancellationToken.None);
                        if (player.Target?.ObjectId != skullObjId) return;
                        SpawnQuestNpc(BeluslanWorldId, 1, PhagrasulNpc, 2851.698f, 160.88698f, 301.78537f, 93);
                    });
                    return false;
                }
                return false;
            }
            if (targetId == ChieftainNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                }
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == ChieftainNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
