// Port of Java data/scripts/system/handlers/quest/empyrean_crucible/_18208IllusionOrInfiltration.java (vlog).
// Elyos side. Started at Inggril (205316); kill mob 217819 four times (var1 0->4), a fifth kill of
// 217819 flips var0 to 1, then a kill of mob 218185 completes to REWARD (var2 1). Turn in at Molfus (205309).
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

namespace Quest.EmpyreanCrucible;

public sealed class _18208IllusionOrInfiltration : QuestHandlerBase
{
    private const int QuestIdConst = 18208;
    private const int StartNpc     = 205316; // Inggril
    private const int TurnInNpc    = 205309; // Molfus
    private const int Mob1         = 217819;
    private const int Mob2         = 218185;

    public _18208IllusionOrInfiltration(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Mob1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob2).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry       = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }
        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != TurnInNpc) return false;
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

        int targetId = env.TargetId;
        int var = entry.GetVar(0);
        if (var == 0)
        {
            int var1 = entry.GetVar(1);
            if (var1 < 4)
            {
                if (targetId != Mob1) return false;
                await ChangeQuestStepAsync(conn, entry, 1, var1 + 1, toReward: false, ct);
                return true;
            }
            if (var1 == 4 && targetId == Mob1)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                return true;
            }
        }
        else if (var == 1 && targetId == Mob2)
        {
            await ChangeQuestStepAsync(conn, entry, 2, 1, toReward: true, ct);
            return true;
        }
        return false;
    }
}
