// Port of Java data/scripts/system/handlers/quest/theobomos/_3048OwneroftheAngledBladeDagger.java.
// Quest-item quest: using the Angled Blade Dagger (182208033) opens the accept dialog (page 4);
// accepting starts it; talk to Yakumo (798208) to advance; turn in at Erisilith (798206).
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

namespace Quest.Theobomos;

public sealed class _3048OwneroftheAngledBladeDagger : QuestHandlerBase
{
    private const int QuestIdConst = 3048;
    private const int YakumoNpc    = 798208;
    private const int ErisilithNpc = 798206;
    private const int DaggerItemId = 182208033;

    private readonly IItemDao _itemDao;

    public _3048OwneroftheAngledBladeDagger(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(YakumoNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ErisilithNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(DaggerItemId, QuestId);
    }

    // Java onItemUseEvent: using the dagger when the quest isn't started opens the accept dialog.
    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != DaggerItemId) return false;
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

        if (targetId == 0 && (entry is null || entry.Status == QuestStatus.NONE))
        {
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.START && targetId == YakumoNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, 1);
                return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: true, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ErisilithNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, DaggerItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
