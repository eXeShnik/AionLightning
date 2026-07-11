// Port of Java data/scripts/system/handlers/quest/verteron/_1131UndeliveredArmor.java (MrPoke).
// Talk to 203097 to start; advance at 799093 (var 0->1); turn in at 203101 (var ->2, REWARD).
using System.Linq;
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

namespace Quest.Verteron;

public sealed class _1131UndeliveredArmor : QuestHandlerBase
{
    private const int QuestIdConst = 1131;
    private const int FirstNpc     = 203097;
    private const int SecondNpc    = 799093;
    private const int ThirdNpc     = 203101;

    public _1131UndeliveredArmor(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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

        if (entry is null)
        {
            if (targetId != FirstNpc) return false;
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == SecondNpc)
            {
                switch (DialogActionLookup.FromId(env.DialogId))
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO1:
                        entry.SetVar(0, entry.GetVar(0) + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                        return true;
                    default:
                        return await SendQuestStartDialogAsync(env, conn, ct);
                }
            }
            if (targetId == ThirdNpc)
            {
                switch (DialogActionLookup.FromId(env.DialogId))
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.SELECT_QUEST_REWARD:
                        entry.SetVar(0, 2);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestEndDialogAsync(env, conn, ct);
                    default:
                        return false;
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == ThirdNpc)
        {
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
