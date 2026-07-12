// Port of Java data/scripts/system/handlers/quest/danaria/_10092OhRightIdgel.java.
// Auto-(re)starts on level-up once 10091 is complete. Dialog chain: garn(800820, var0->1) ->
// erirunerk(800830, var1->2) -> kaza(800831, checks quest item 10000, var2->3) -> the "hyperion"
// object (730737, var3(sic, actually var4)->reward). Turn-in at kaza2 (800832).
// Skip vs Java: SETPRO's var==4 use_object branch calls TeleportService2.teleportTo to move the
// player back to 600060000 after playing movie 853 - no TeleportService2 exists in this port. The
// var/status transition (var stays 4, flips to REWARD) is still ported so the quest completes.
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

namespace Quest.Danaria;

public sealed class _10092OhRightIdgel : QuestHandlerBase
{
    private const int QuestIdConst   = 10092;
    private const int PrecedingQuest = 10091;
    private const int GarnNpc        = 800820;
    private const int ErirunerkNpc   = 800830;
    private const int KazaNpc        = 800831;
    private const int HyperionObj    = 730737;
    private const int Kaza2Npc       = 800832;

    private readonly IItemDao _itemDao;

    public _10092OhRightIdgel(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(GarnNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ErirunerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KazaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HyperionObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Kaza2Npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, PrecedingQuest, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == GarnNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == ErirunerkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == KazaNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return var switch
                        {
                            2 => await SendQuestDialogAsync(conn, targetObjId, 1693, ct),
                            3 => await SendQuestDialogAsync(conn, targetObjId, 2034, ct),
                            _ => false
                        };
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 3, 4, false, 10000, 10001, ct);
                    case DialogAction.SETPRO3:
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (targetId == HyperionObj)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 4)
                {
                    await PlayQuestMovieAsync(conn, player, 853, ct);
                    return await DefaultCloseDialogAsync(env, conn, 4, 4, reward: true, sameNpc: false, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == Kaza2Npc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
