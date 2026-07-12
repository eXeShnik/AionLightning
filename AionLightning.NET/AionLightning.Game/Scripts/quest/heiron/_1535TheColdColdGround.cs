// Port of Java data/scripts/system/handlers/quest/heiron/_1535TheColdColdGround.java.
// Talk to 204580 to start; bring in one of three pelt types (5x182201818, 3x182201819, or
// 1x182201820) — whichever the player is carrying picks the reward tier (var 1/2/3) and an extra
// potion hand-out on top of the standard reward.
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

namespace Quest.Heiron;

public sealed class _1535TheColdColdGround : QuestHandlerBase
{
    private const int QuestIdConst = 1535;
    private const int StartNpc = 204580;
    private const int AbexPeltItem   = 182201818;
    private const int WorgPeltItem   = 182201819;
    private const int KarnifPeltItem = 182201820;
    private const int ManaPotionItem = 162000010;
    private const int LifeSerumItem  = 162000015;

    private readonly IItemDao _itemDao;

    public _1535TheColdColdGround(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (env.TargetId != StartNpc) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;

        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            bool abexSkins   = player.Inventory.FindByItemId(AbexPeltItem) is { Count: > 4 };
            bool worgSkins   = player.Inventory.FindByItemId(WorgPeltItem) is { Count: > 2 };
            bool karnifSkins = player.Inventory.FindByItemId(KarnifPeltItem) is { Count: > 0 };

            if (dialog is DialogAction.USE_OBJECT or DialogAction.QUEST_SELECT)
            {
                if (abexSkins || worgSkins || karnifSkins)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }
            else if (dialog == DialogAction.SETPRO1 && abexSkins)
            {
                entry.SetVar(0, 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            else if (dialog == DialogAction.SETPRO2 && worgSkins)
            {
                entry.SetVar(0, 2);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
            }
            else if (dialog == DialogAction.SETPRO3 && karnifSkins)
            {
                entry.SetVar(0, 3);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 7, ct);
            }
            return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int var = entry.GetVar(0);
            if (var == 1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, AbexPeltItem, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (var == 2)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, ManaPotionItem, 5, ct))
                {
                    entry.Status = QuestStatus.START;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                await RemoveQuestItemAsync(player, conn, _itemDao, WorgPeltItem, 3, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (var == 3)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, LifeSerumItem, 5, ct))
                {
                    entry.Status = QuestStatus.START;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return true;
                }
                await RemoveQuestItemAsync(player, conn, _itemDao, KarnifPeltItem, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
        }
        return false;
    }
}
