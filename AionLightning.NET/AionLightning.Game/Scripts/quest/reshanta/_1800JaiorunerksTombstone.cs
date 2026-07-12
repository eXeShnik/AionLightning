// Port of Java data/scripts/system/handlers/quest/reshanta/_1800JaiorunerksTombstone.java (Gigi,
// reworked vlog). Start at 279016 (gives item 182202163 on accept). Use the tomb (730141) with the
// item to remove it and advance var0 0->1; turn in at 279016.
// Fixed 2 latent Java bugs (both were unguarded switch-case fallthrough in the original): (1) the
// tomb's USE_OBJECT case fell through into the var-advance case whenever the player lacked the
// pickaxe item or var wasn't 0, silently completing the step without the item requirement — fixed
// by gating the advance on var==0 and only showing the confirm dialog when the item is present;
// (2) the turn-in npc's QUEST_SELECT case fell through into the reward-flip case whenever var != 1,
// letting a player skip straight to REWARD before ever visiting the tomb — fixed by gating the
// reward flip on var==1, matching the dialog's own condition.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Reshanta;

public sealed class _1800JaiorunerksTombstone : QuestHandlerBase
{
    private const int QuestIdConst = 1800;
    private const int StartNpc     = 279016;
    private const int TombNpc      = 730141;
    private const int ItemId       = 182202163;

    private readonly IItemDao _itemDao;

    public _1800JaiorunerksTombstone(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TombNpc).OnTalk.Add(QuestId);
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
            if (await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct))
                return await SendQuestStartDialogAsync(env, conn, ct);
            return true;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == TombNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 0)
                {
                    if (player.Inventory.FindByItemId(ItemId) is { Count: > 0 })
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1 && var == 0)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
            }
            else if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD && var == 1)
                {
                    await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
