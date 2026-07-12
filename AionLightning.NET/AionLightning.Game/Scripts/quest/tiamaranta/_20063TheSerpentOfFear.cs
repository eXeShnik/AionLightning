// Port of Java data/scripts/system/handlers/quest/tiamaranta/_20063TheSerpentOfFear.java (vlog).
// Asmodian mirror of _10063ShotThroughTheHeart, same shape: talk to Garnon (800018) to advance var
// 0->1 (SETPRO1); entering HEART_OF_PETRIFICATION_ENTRANCE flips 1->2; talk to Benjal (800069) at
// var 2 to flip 2->3 (SETPRO3); use quest object 701237 at var 3 to receive the collect item
// (182212560); back at Garnon, CHECK_USER_HAS_QUEST_ITEM collects it and flips to REWARD; turn in
// at Adella (205886).
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

namespace Quest.Tiamaranta;

public sealed class _20063TheSerpentOfFear : QuestHandlerBase
{
    private const int QuestIdConst = 20063;
    private const int GarnonNpc    = 800018;
    private const int BenjalNpc    = 800069;
    private const int AdellaNpc    = 205886;
    private const int UseObjectNpc = 701237;
    private const int CollectItemId = 182212560;
    private const string HeartEntranceZone = "HEART_OF_PETRIFICATION_ENTRANCE_600030000";

    private readonly IItemDao _itemDao;

    public _20063TheSerpentOfFear(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GarnonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BenjalNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AdellaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UseObjectNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, HeartEntranceZone);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == GarnonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 3, 3, reward: true, checkOkId: 10000, checkFailId: 10001, ct);
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            else if (targetId == BenjalNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            }
            else if (targetId == UseObjectNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 3)
                    await GiveQuestItemAsync(player, conn, _itemDao, CollectItemId, 1, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == AdellaNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != HeartEntranceZone) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }
}
