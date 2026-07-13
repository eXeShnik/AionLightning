// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21080MessageInAWindstream.java (Cheatkiller).
// Starts on talking to 799231 (gives item 182207939 on accept); entering Antagor Canyon bumps
// var0 by one, up to three times (0->1->2->3); talking to 799427 at var0==3 removes the item and
// advances to var0==4; entering Gelkmaros Fortress at var0==4 flips to REWARD; turn in at 799231.
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

public sealed class _21080MessageInAWindstream : QuestHandlerBase
{
    private const int QuestIdConst = 21080;
    private const int StartNpc     = 799231;
    private const int RelayNpc     = 799427;
    private const int GivenItem    = 182207939;
    private const string CanyonZone   = "ANTAGOR_CANYON_220070000";
    private const string FortressZone = "GELKMAROS_FORTRESS_220070000";

    private readonly IItemDao _itemDao;

    public _21080MessageInAWindstream(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, CanyonZone);
        RegisterOnEnterZone(engine, FortressZone);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;

            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);

            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                if (player.Quests.Contains(QuestId)) return false;
                if (!await GiveQuestItemAsync(player, conn, _itemDao, GivenItem, 1, ct)) return false;

                var newEntry = new QuestEntry { QuestId = QuestId, Status = QuestStatus.START };
                player.Quests.Add(newEntry);
                await QuestDao.UpsertAsync(player.ObjectId, newEntry, ct);
                await conn.SendAsync(new SM_QUEST_ACTION(newEntry.QuestId,
                    SM_QUEST_ACTION.ActionType.Accept, (byte)newEntry.Status, newEntry.Step), ct);
                await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);

                return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            }

            if (dialog is DialogAction.QUEST_REFUSE or DialogAction.QUEST_REFUSE_1
                or DialogAction.QUEST_REFUSE_2 or DialogAction.QUEST_REFUSE_SIMPLE)
                return await CloseDialogWindowAsync(conn, targetObjId, ct);

            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != RelayNpc) return false;

            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 3)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO4)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, reward: false, sameNpc: false,
                    0, 0, GivenItem, 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (zoneName == CanyonZone)
        {
            if (var >= 3) return false;
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (zoneName == FortressZone && var == 4)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: true, ct);
            return true;
        }
        return false;
    }
}
