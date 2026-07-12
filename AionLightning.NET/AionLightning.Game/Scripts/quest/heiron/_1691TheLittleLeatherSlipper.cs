// Port of Java data/scripts/system/handlers/quest/heiron/_1691TheLittleLeatherSlipper.java.
// Talk to 798386 to start; 790005 advances var0->1, 798386 again var1->2, then use object 700563
// (var2->3, flips to REWARD); turn in at 798386.
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

namespace Quest.Heiron;

public sealed class _1691TheLittleLeatherSlipper : QuestHandlerBase
{
    private const int QuestIdConst = 1691;
    private const int StartNpc  = 798386;
    private const int MidNpc    = 790005;
    private const int ObjectNpc = 700563;

    public _1691TheLittleLeatherSlipper(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == MidNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2 && var == 1)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            }
            else if (targetId == ObjectNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    entry.SetVar(0, var + 1);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
