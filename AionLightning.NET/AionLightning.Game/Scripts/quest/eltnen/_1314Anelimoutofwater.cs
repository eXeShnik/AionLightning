// Port of Java data/scripts/system/handlers/quest/eltnen/_1314Anelimoutofwater.java (MrPoke, remod).
// Talk to 730023 to start; advance at 730021 (var 0->1); turn in at 730014 (var ->2, REWARD).
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

public sealed class _1314Anelimoutofwater : QuestHandlerBase
{
    private const int QuestIdConst = 1314;
    private const int FirstNpc     = 730023;
    private const int SecondNpc    = 730021;
    private const int ThirdNpc     = 730014;

    public _1314Anelimoutofwater(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
            if (entry is null)
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
