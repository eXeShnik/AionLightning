// Port of Java data/scripts/system/handlers/quest/danaria/_20092TheNicestDaevainDanaria.java
// (Asmodian mirror of _10092OhRightIdgel). Auto-(re)starts on level-up once 20091 is complete.
// Dialog chain: kolfinn(800825, var0->1) -> erirunerk(800830, var1->2) -> lucullus(800834, checks
// quest item 10000, var2->3) -> the "hyperion" object (730737, var4->reward). Turn-in at
// lucullus's second spawn (800835).
// Skip vs Java: the var==4 use_object branch calls TeleportService2.teleportTo to move the player
// back to 600060000 after playing movie 854 - no TeleportService2 exists in this port. The
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

public sealed class _20092TheNicestDaevainDanaria : QuestHandlerBase
{
    private const int QuestIdConst   = 20092;
    private const int PrecedingQuest = 20091;
    private const int KolfinnNpc     = 800825;
    private const int ErirunerkNpc   = 800830;
    private const int LucullusNpc    = 800834;
    private const int HyperionObj    = 730737;
    private const int Lucullus2Npc   = 800835;

    private readonly IItemDao _itemDao;

    public _20092TheNicestDaevainDanaria(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(KolfinnNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ErirunerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LucullusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HyperionObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Lucullus2Npc).OnTalk.Add(QuestId);
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
            if (targetId == KolfinnNpc)
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
            if (targetId == LucullusNpc)
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
                    await PlayQuestMovieAsync(conn, player, 854, ct);
                    return await DefaultCloseDialogAsync(env, conn, 4, 4, reward: true, sameNpc: false, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == Lucullus2Npc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
