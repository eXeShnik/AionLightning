// Port of Java data/scripts/system/handlers/quest/ishalgen/_2122AshesToAshes.java.
// Item-use start (182203120); talk to Suder (203551); use the urn (730029) — if holding the
// ashes (182203133), consume them (SELECT_ACTION_1353) and advance (SETPRO2 → REWARD); loot
// object 700148; turn in at Suder.
// Skip vs Java: registerCanAct(700148) (an onCanAct gate) is not modelled — the object simply
// acts; harmless.
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

namespace Quest.Ishalgen;

public sealed class _2122AshesToAshes : QuestHandlerBase
{
    private const int QuestIdConst = 2122;
    private const int SuderNpc     = 203551;
    private const int LootObj      = 700148;
    private const int UrnObj       = 730029;
    private const int StartItemId  = 182203120;
    private const int AshesItemId  = 182203133;

    private readonly IItemDao _itemDao;

    public _2122AshesToAshes(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(StartItemId, QuestId);
        engine.RegisterQuestNpc(SuderNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LootObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UrnObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != StartItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE)
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == 0 && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
        {
            await StartMissionAsync(conn, player, QuestStatus.START, ct);
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
            return true;
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == SuderNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SELECT_ACTION_1012:
                        await RemoveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                    case DialogAction.SETPRO1:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            }
            if (targetId == UrnObj)
            {
                switch (dialog)
                {
                    case DialogAction.USE_OBJECT:
                        if ((player.Inventory.FindByItemId(AshesItemId)?.Count ?? 0) >= 1)
                            return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SELECT_ACTION_1353:
                        await RemoveQuestItemAsync(player, conn, _itemDao, AshesItemId, 1, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    case DialogAction.SETPRO2:
                        return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: false, ct);
                    default:
                        return false;
                }
            }
            if (targetId == LootObj) return true;
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == SuderNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
