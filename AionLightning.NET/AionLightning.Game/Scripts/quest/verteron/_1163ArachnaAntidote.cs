// Port of Java data/scripts/system/handlers/quest/verteron/_1163ArachnaAntidote.java
// (Balthazar). Talk to Suania (203096) to start; advance at Meiritin (203151, var 0->1); turn in
// at Rustan (203155).
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

public sealed class _1163ArachnaAntidote : QuestHandlerBase
{
    private const int QuestIdConst = 1163;
    private const int SuaniaNpc    = 203096;
    private const int MeiritinNpc  = 203151;
    private const int RustanNpc    = 203155;

    public _1163ArachnaAntidote(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SuaniaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SuaniaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MeiritinNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RustanNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null)
        {
            if (targetId == SuaniaNpc)
            {
                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == MeiritinNpc)
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
                        return await SendQuestEndDialogAsync(env, conn, ct);
                }
            }
            if (targetId == RustanNpc)
            {
                switch (DialogActionLookup.FromId(env.DialogId))
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.SELECT_QUEST_REWARD:
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                        return true;
                    default:
                        return await SendQuestEndDialogAsync(env, conn, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == RustanNpc)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
