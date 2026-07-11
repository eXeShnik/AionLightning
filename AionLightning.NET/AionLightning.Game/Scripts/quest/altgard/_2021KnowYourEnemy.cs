// Port of Java data/scripts/system/handlers/quest/altgard/_2021KnowYourEnemy.java (MrPoke).
// Talk to 203669, kill mob 700099 (var 3->6), turn in at Suthran (203557). Zone-mission chain,
// level-up gated.
// Skip vs Java: entering BLACK_CLAW_OUTPOST_220030000 used to bump var 1->2 (no zone-shape system
// in this port) — folded into the SETPRO1 dialog transition (var goes 0 -> 2 directly). The
// disguise buff (SkillEngine.applyEffectDirectly/removeEffect on skill 1868) is cosmetic combat
// flavor with no completion-gating role and is omitted.
using System.Linq;
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

namespace Quest.Altgard;

public sealed class _2021KnowYourEnemy : QuestHandlerBase
{
    private const int QuestIdConst = 2021;
    private const int FirstNpc     = 203669;
    private const int SuthranNpc   = 203557;
    private const int SpyMob       = 700099;

    public _2021KnowYourEnemy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SpyMob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(SuthranNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2200, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;
        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == FirstNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.QUEST_SELECT when var == 6:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SETPRO1:
                        return await DefaultCloseDialogAsync(env, conn, 0, 2, ct); // 0 -> 2 (zone-gate collapsed)
                    case DialogAction.SETPRO2:
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    case DialogAction.SETPRO3:
                        return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
                    default:
                        return false;
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == SuthranNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, SpyMob, startVar: 3, endVar: 6, ct);
}
