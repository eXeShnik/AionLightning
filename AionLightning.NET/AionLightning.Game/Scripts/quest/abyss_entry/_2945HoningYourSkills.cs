// Port of Java data/scripts/system/handlers/quest/abyss_entry/_2945HoningYourSkills.java (Hellboy aion4Free; reworked vlog).
// Talk to Balder (204075, var 0->1), then Therf (204088, var 1->reward); report back to Balder.
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

public sealed class _2945HoningYourSkills : QuestHandlerBase
{
    private const int QuestIdConst = 2945;
    private const int BalderNpc    = 204075;
    private const int TherfNpc     = 204088;

    public _2945HoningYourSkills(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(BalderNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TherfNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

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
            if (targetId != BalderNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        switch (targetId)
        {
            case BalderNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO1:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            case TherfNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SET_SUCCEED:
                        return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: false, ct);
                    default:
                        return false;
                }
            default:
                return false;
        }
    }
}
