// Port of Java data/scripts/system/handlers/quest/ishalgen/_2123TheImprisonedGourmet.java.
// Munin (203550) start; loot the Methu Egg object (700128); hand back one of three dishes
// (182203121/122/123 → var 5/6/7) and finish with the matching reward tier.
using System.Linq;
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

namespace Quest.Ishalgen;

public sealed class _2123TheImprisonedGourmet : QuestHandlerBase
{
    private const int QuestIdConst = 2123;
    private const int MuninNpc     = 203550;
    private const int EggObj       = 700128;

    private readonly IItemDao _itemDao;

    public _2123TheImprisonedGourmet(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MuninNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MuninNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EggObj).OnTalk.Add(QuestId);
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
            if (targetId == MuninNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == MuninNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO1:
                        return await HandInAsync(player, conn, entry, targetObjId, 182203121, 5, 5, ct);
                    case DialogAction.SETPRO2:
                        return await HandInAsync(player, conn, entry, targetObjId, 182203122, 6, 6, ct);
                    case DialogAction.SETPRO3:
                        return await HandInAsync(player, conn, entry, targetObjId, 182203123, 7, 7, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            if (targetId == EggObj) return true;
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == MuninNpc)
        {
            int var = entry.GetVar(0);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await FinishQuestAsync(conn, player, var - 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    private async ValueTask<bool> HandInAsync(Player player, GsClientConnection conn, QuestEntry entry,
        int targetObjId, int dishItemId, int var, int dialogPage, CancellationToken ct)
    {
        if ((player.Inventory.FindByItemId(dishItemId)?.Count ?? 0) < 1)
            return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);

        entry.SetVar(0, var);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        await RemoveQuestItemAsync(player, conn, _itemDao, dishItemId, 1, ct);
        return await SendQuestDialogAsync(conn, targetObjId, dialogPage, ct);
    }
}
