// Port of Java data/scripts/system/handlers/quest/reshanta/_4718PressingTheAttack.java.
// Asmodian mirror of _3718DredgingTheDredgion. Start/relay/turn-in all at 278001. var0 gates the
// intro relay (0 -> SETPRO1 -> 1). var1 is a dredgion-run counter (0->3, one per dredgion completion);
// var2 is a kill counter (0->8, mob 214814). Turn-in opens once var1==3 && var2==8; SELECT_QUEST_REWARD
// then flips var0 1->1 straight to REWARD.
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

namespace Quest.Reshanta;

public sealed class _4718PressingTheAttack : QuestHandlerBase
{
    private const int QuestIdConst = 4718;
    private const int Npc          = 278001;
    private const int KillNpc      = 214814;

    public _4718PressingTheAttack(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        RegisterOnDredgionReward(engine);
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            int var1 = entry.GetVar(1);
            int var2 = entry.GetVar(2);
            if (targetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (entry.GetVar(0) == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var1 == 3 && var2 == 8)
                        return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                }
                else if (dialog == DialogAction.SETPRO1)
                {
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                else if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: true, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Npc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // Java: defaultOnKillEvent(env, 214814, 0, 8, 2) — counts on quest var 2 (0..8).
        // Inlined on var 2 because the base helper's span overload hardcodes var 0 (no varNum overload).
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != KillNpc) return false;

        int var2 = entry.GetVar(2);
        if (var2 < 0 || var2 >= 8) return false;

        await ChangeQuestStepAsync(conn, entry, 2, var2 + 1, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDredgionRewardAsync(QuestEnv env, int rank, GsClientConnection conn, CancellationToken ct)
    {
        // Java ignores the dredgion result rank here — any completion advances the var1 counter (0..3).
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var1 = entry.GetVar(1);
        if (var1 >= 3) return false;

        await ChangeQuestStepAsync(conn, entry, 1, var1 + 1, toReward: false, ct);
        return true;
    }
}
