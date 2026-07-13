// Port of Java data/scripts/system/handlers/quest/abyss_entry/_2946AbyssGeneralKnowledge.java (Hellboy aion4Free).
// Talk chain: 204075 (var 0->1) -> 204210 (1->2) -> 204211 (2->3) -> 204208 (3->reward);
// turn in at 204053. Level-up gated on quest 2945.
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

namespace Quest.AbyssEntry;

public sealed class _2946AbyssGeneralKnowledge : QuestHandlerBase
{
    private const int QuestIdConst = 2946;
    private const int Npc1         = 204075;
    private const int Npc2         = 204210;
    private const int Npc3         = 204211;
    private const int Npc4         = 204208;
    private const int TurnInNpc    = 204053;

    public _2946AbyssGeneralKnowledge(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(Npc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc2).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc3).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc4).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2945, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != TurnInNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        switch (targetId)
        {
            case Npc1:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO1:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            case Npc2:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO2:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    default:
                        return false;
                }
            case Npc3:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SETPRO3:
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    default:
                        return false;
                }
            case Npc4:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 3:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.SET_SUCCEED:
                        return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: false, ct);
                    default:
                        return false;
                }
            default:
                return false;
        }
    }
}
