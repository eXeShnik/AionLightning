// Port of Java data/scripts/system/handlers/quest/esoterrace/_18407GroupDrakanJournalism.java
// (Ritsu). Start at 799552 (dialog 1011); 799552 again bumps var 0 -> 1 via SETPRO2 at dialog 1693;
// turn in at 799553 once var 0 == 2.
// Java bug (ported as-is): once accepted, var 0 stays at 0 (freshly-created quest state) but talking
// to 799552 again only has a branch for `status == START && var 0 == 1` - there is no `var 0 == 0`
// case at all, so nothing responds and the dialog falls through to "unhandled" until var somehow
// reaches 1 (which nothing in this handler ever does). The quest is startable but not otherwise
// progressable through this handler; kept exactly as Java has it since the missing branch isn't an
// "obvious" one-line fix (no evidence of what the intended var-0 transition dialog/page should be).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Esoterrace;

public sealed class _18407GroupDrakanJournalism : QuestHandlerBase
{
    private const int QuestIdConst = 18407;
    private const int StartNpc     = 799552;
    private const int TurnInNpc    = 799553;

    public _18407GroupDrakanJournalism(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START && entry.GetVar(0) == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == TurnInNpc)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry is not null && entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
