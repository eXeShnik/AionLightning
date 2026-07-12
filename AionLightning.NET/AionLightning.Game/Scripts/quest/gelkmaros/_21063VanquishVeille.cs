// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21063VanquishVeille.java (Cheatkiller).
// Item-use quest: using item 182207852 opens the accept dialog (page 4); talk to 799354 (var 0->1);
// kill 281433 once while at var 1 advances to var 2; back at 799354 (SET_SUCCEED gives item
// 182207853, var 2->3, reward); turn in at 799225 (removes both items).
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

namespace Quest.Gelkmaros;

public sealed class _21063VanquishVeille : QuestHandlerBase
{
    private const int QuestIdConst = 21063;
    private const int RelayNpc     = 799354;
    private const int FinalNpc     = 799225;
    private const int MobId        = 281433;
    private const int QuestItemId  = 182207852;
    private const int TrophyItemId = 182207853;

    private readonly IItemDao _itemDao;

    public _21063VanquishVeille(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(QuestItemId, QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobId).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(FinalNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != QuestItemId) return false;
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

        if (targetId == 0)
        {
            if (entry is null && env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            return false;
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.START && targetId == RelayNpc)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SET_SUCCEED)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, TrophyItemId, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 2, 3, reward: true, sameNpc: false, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == FinalNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, QuestItemId, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, TrophyItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MobId, startVar: 1, endVar: 2, ct);
}
