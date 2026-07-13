// Port of Java data/scripts/system/handlers/quest/beluslan/_2600HumongousMalek.java.
// Talk to 204734 to start; 798119 gives 182204528 (var0->1); using the summoning stone 700512
// while carrying it spawns 215383 and consumes the stone; turning in at 204734 removes 182204529
// and flips to REWARD.
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

public sealed class _2600HumongousMalek : QuestHandlerBase
{
    private const int QuestIdConst = 2600;
    private const int StartNpc = 204734;
    private const int Npc119 = 798119;
    private const int SummonStoneObj = 700512;
    private const int SummoningItem = 182204528;
    private const int RewardTicketItem = 182204529;
    private const int BeluslanWorldId = 220040000;
    private const int MalekNpc = 215383;

    private readonly IItemDao _itemDao;

    public _2600HumongousMalek(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc119).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SummonStoneObj).OnTalk.Add(QuestId);
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

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc) return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == Npc119)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, SummoningItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            return false;
        }
        if (targetId == SummonStoneObj)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 1)
            {
                var stone = player.Inventory.FindByItemId(SummoningItem);
                if (stone is not null && stone.Count == 1)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, SummoningItem, 1, ct);
                    SpawnQuestNpc(BeluslanWorldId, 1, MalekNpc, 1140.78f, 432.85f, 341.0825f, 0);
                    return true;
                }
            }
            return false;
        }
        if (targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD && var == 1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, RewardTicketItem, 1, ct);
                entry.SetVar(0, 2);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }
        return false;
    }
}
