// Port of Java data/scripts/system/handlers/quest/danaria/_10091EvenBalaurGetTheBlues.java.
// Auto-(re)starts on level-up once 10090 is complete. Dialog chain at Hamazi (801154, var 0->1
// via SETPRO1, var 1->2 via CHECK_USER_HAS_QUEST_ITEM, var 2->3 via SETPRO3); a kill grind then
// bumps var 3..6 on mob 231418 and the last kill of boss mob 231417 (only once var==6) flips to
// reward. Java bug fixed: the original onKillEvent switch had no break after the 231418 case, so
// killing 231418 while var was NOT in [2,5] fell through into the 231417 reward check (killing the
// wrong mob could reward the quest); ported as two independent checks instead.
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

public sealed class _10091EvenBalaurGetTheBlues : QuestHandlerBase
{
    private const int QuestIdConst  = 10091;
    private const int PrecedingQuest = 10090;
    private const int HamaziNpc     = 801154;
    private const int GrindMobNpc   = 231418;
    private const int BossMobNpc    = 231417;

    private readonly IItemDao _itemDao;

    public _10091EvenBalaurGetTheBlues(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(HamaziNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GrindMobNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(BossMobNpc).OnKill.Add(QuestId);
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

        if (entry.Status == QuestStatus.START && targetId == HamaziNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return var switch
                    {
                        0 => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                        1 => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                        2 => await SendQuestDialogAsync(conn, targetObjId, 1694, ct),
                        _ => false
                    };
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                case DialogAction.SETPRO3:
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, false, 1694, 1693, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == HamaziNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (env.TargetId == GrindMobNpc && var is >= 2 and <= 5)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (env.TargetId == BossMobNpc && var == 6)
        {
            await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
            return true;
        }
        return false;
    }
}
