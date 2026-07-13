// Port of Java data/scripts/system/handlers/quest/beluslan/_2051SavingBeluslanFortress.java.
// Zone-mission opener: talk to 204702 (var0->1); parallel branches converge on 204733 (var1->11,
// var2->3, var6->7), 204206 (var3->4), 278040 (var4/5, collect-item check gives 182204302 and
// bumps var to 5, then var4->5 again via SETPRO5); use-object 700285 at var7 removes the item and
// flips to REWARD; use-object 700284 at var11 resets to var2 (Java: useQuestObject(env, 11, 2,
// false, 0) — ported as-is even though it steps the counter backwards).
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

namespace Quest.Beluslan;

public sealed class _2051SavingBeluslanFortress : QuestHandlerBase
{
    private const int QuestIdConst = 2051;
    private const int Npc702 = 204702;
    private const int Npc733 = 204733;
    private const int Npc206 = 204206;
    private const int Npc040 = 278040;
    private const int Obj285 = 700285;
    private const int Obj284 = 700284;
    private const int CollectItem = 182204302;

    private readonly IItemDao _itemDao;

    public _2051SavingBeluslanFortress(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(CollectItem, QuestId);
        engine.RegisterQuestNpc(Npc702).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc733).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc206).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc040).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Obj285).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Obj284).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Npc702)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == Npc702)
        {
            if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }
        if (targetId == Npc733)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 11, ct);
            if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            if (dialog == DialogAction.SETPRO7) return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
            return false;
        }
        if (targetId == Npc206)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 3)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            return false;
        }
        if (targetId == Npc040)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                return false;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 5, false, 10000, 10001, CollectItem, 1, ct);
            if (dialog == DialogAction.SETPRO5) return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            return false;
        }
        if (targetId == Obj285)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 7)
                return await UseQuestObjectAsync(env, conn, 7, 7, true, 0, 0, 0, CollectItem, 1, 0, false, _itemDao, ct);
            return false;
        }
        if (targetId == Obj284)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 11)
                return await UseQuestObjectAsync(env, conn, 11, 2, false, 0, ct);
            return false;
        }
        return false;
    }
}
