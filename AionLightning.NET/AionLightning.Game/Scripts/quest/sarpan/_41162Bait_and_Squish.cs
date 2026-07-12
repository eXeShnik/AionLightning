// Port of Java data/scripts/system/handlers/quest/sarpan/_41162Bait_and_Squish.java (Cheatkiller).
// Talk to 205567 to start (gives item 182213212); use object 730470 (var 0->1, spawns mob 218652
// at the object's position); kill 218652 (var==1 flips to REWARD); turn in at 205583 (removes the
// item first).
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

namespace Quest.Sarpan;

public sealed class _41162Bait_and_Squish : QuestHandlerBase
{
    private const int QuestIdConst = 41162;
    private const int StartNpc      = 205567;
    private const int BaitNpc       = 730470;
    private const int TurnInNpc     = 205583;
    private const int MobId         = 218652;
    private const int ItemId        = 182213212;

    private readonly IItemDao _itemDao;

    public _41162Bait_and_Squish(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(BaitNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobId).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MobId, startVar: 1, reward: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await StartWithItemAsync(player, conn, targetObjId, dialog, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == BaitNpc)
        {
            if (dialog == DialogAction.USE_OBJECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && env.Target is not null)
            {
                var pos = env.Target.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, MobId, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    private async ValueTask<bool> StartWithItemAsync(Player player, GsClientConnection conn, int targetObjId, DialogAction dialog, CancellationToken ct)
    {
        switch (dialog)
        {
            case DialogAction.QUEST_ACCEPT:
            case DialogAction.QUEST_ACCEPT_1:
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            case DialogAction.QUEST_ACCEPT_SIMPLE:
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            case DialogAction.QUEST_REFUSE_1:
            case DialogAction.QUEST_REFUSE_2:
                return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
            case DialogAction.QUEST_REFUSE_SIMPLE:
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            default:
                return false;
        }
    }
}
