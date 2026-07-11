// Port of Java data/scripts/system/handlers/quest/eltnen/_1452DoubtfulCharacters.java (MrPoke, remod).
// Talk to 203934 to start; advance at 203834 (var 0->1); turn in at 203704 (var ->2, REWARD).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Eltnen;

public sealed class _1452DoubtfulCharacters : QuestHandlerBase
{
    private const int QuestIdConst = 1452;
    private const int FirstNpc     = 203934;
    private const int SecondNpc    = 203834;
    private const int ThirdNpc     = 203704;

    public _1452DoubtfulCharacters(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(FirstNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == FirstNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == SecondNpc)
        {
            if (entry is not { Status: QuestStatus.START } || entry.GetVar(0) != 0) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == ThirdNpc)
        {
            if (entry is null) return false;
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD && entry.Status is not (QuestStatus.COMPLETE or QuestStatus.NONE))
            {
                entry.SetVar(0, 2);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
