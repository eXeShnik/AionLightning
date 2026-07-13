// Port of Java data/scripts/system/handlers/quest/terath_dredgion/_30610TheGoodNewsAndBad.java (Ritsu).
// Dredgion kill quest (counterpart of _30600): accept at Skafir (205864); flavour talk at Astella
// (800327, SETPRO1); kill navigators (219256/219257 sets var 0->1, then 219256/219257/219264 flips to
// REWARD); turn in at Skafir (SELECT_QUEST_REWARD while var==1 -> REWARD, reward page 5).
// Java's repeat guard qs.canRepeat() isn't ported — same omission as every other repeatable dredgion
// quest already ported here (e.g. chantra_dredgion/_3722MyNewToy).
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

namespace Quest.TerathDredgion;

public sealed class _30610TheGoodNewsAndBad : QuestHandlerBase
{
    private const int QuestIdConst = 30610;
    private const int Skafir       = 205864;
    private const int Astella      = 800327;
    private const int Nav1         = 219256;
    private const int Nav2         = 219257;
    private const int Nav3         = 219264;

    public _30610TheGoodNewsAndBad(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Skafir).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Skafir).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Astella).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Nav1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Nav2).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Nav3).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != Skafir) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == Astella)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 0, ct);
                // Java switch fallthrough: 800327 falls into 205864
            }

            if (targetId == Astella || targetId == Skafir)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD && var == 1)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Skafir)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        int var = entry.GetVar(0);

        if ((targetId == Nav1 || targetId == Nav2) && var == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            return true;
        }

        // Java switch fallthrough: 219256/219257 fall into 219264
        if ((targetId == Nav1 || targetId == Nav2 || targetId == Nav3) && var == 1)
        {
            await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
            return true;
        }

        return false;
    }
}
