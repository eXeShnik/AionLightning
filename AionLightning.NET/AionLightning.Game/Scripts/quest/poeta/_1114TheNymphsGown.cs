// Port of Java data/scripts/system/handlers/quest/poeta/_1114TheNymphsGown.java.
// Item-use start (Namus's Diary 182200214) → talk to Namus (203075), use Seirenia's clothes
// (700008) to obtain the Nymph's Dress (182200217), then turn in at Namus (var 4) or Asteros
// (203058, var 3). Two-path branch preserved.
// Skips vs Java (documented): using the clothes makes Seirenia (npc 203175) aggro the player
// (getAggroList().addDamage) — omitted because this handler has no NpcAiService reference; the
// dress is still granted and the quest completes. SM_ITEM_USAGE_ANIMATION cosmetic broadcast
// also omitted (no such packet yet).
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Poeta;

public sealed class _1114TheNymphsGown : QuestHandlerBase
{
    private const int QuestIdConst = 1114;
    private const int NamusNpc     = 203075;
    private const int AsterosNpc   = 203058;
    private const int ClothesObj   = 700008;
    private const int DiaryItemId  = 182200214;
    private const int WorkItemId   = 182200226;
    private const int DressItemId  = 182200217;

    private readonly IItemDao _itemDao;

    public _1114TheNymphsGown(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(DiaryItemId, QuestId);
        engine.RegisterQuestNpc(NamusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AsterosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ClothesObj).OnTalk.Add(QuestId);
    }

    // Java onItemUseEvent: using the diary starts the quest (gives the work item, consumes diary).
    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != DiaryItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (await StartMissionAsync(conn, player, QuestStatus.START, ct))
            {
                await GiveQuestItemAsync(player, conn, _itemDao, WorkItemId, 1, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, DiaryItemId, 1, ct);
            }
        }
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        // REWARD turn-in branches
        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == NamusNpc && var == 4)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (targetId == AsterosNpc && var == 3)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        if (entry.Status != QuestStatus.START) return false;

        if (targetId == NamusNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return var switch
                    {
                        0 => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                        2 => await SendQuestDialogAsync(conn, targetObjId, 1693, ct),
                        3 => await SendQuestDialogAsync(conn, targetObjId, 2375, ct),
                        _ => false,
                    };
                case DialogAction.SELECT_QUEST_REWARD when var is 2 or 3:
                    entry.SetVar(0, 4);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, DressItemId, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                case DialogAction.SETPRO1 when var == 0:
                    entry.SetVar(0, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, WorkItemId, 1, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                case DialogAction.SETPRO2 when var == 2:
                    entry.SetVar(0, 3);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                default:
                    return false;
            }
        }

        if (targetId == ClothesObj && dialog == DialogAction.USE_OBJECT && var == 1)
        {
            // Skipped: Seirenia (203175) aggro on the player (no NpcAiService here).
            await GiveQuestItemAsync(player, conn, _itemDao, DressItemId, 1, ct);
            entry.SetVar(0, 2);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }

        if (targetId == AsterosNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                case DialogAction.SETPRO3 when var == 3:
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, DressItemId, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                case DialogAction.SETPRO2 when var == 3:
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                default:
                    return false;
            }
        }
        return false;
    }
}
