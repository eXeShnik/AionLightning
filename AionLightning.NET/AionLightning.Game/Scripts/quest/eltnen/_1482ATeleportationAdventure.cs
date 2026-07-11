// Port of Java data/scripts/system/handlers/quest/eltnen/_1482ATeleportationAdventure.java (Balthazar).
// Talk to Onesimus (203919) to start; Sonirim (203337) advances var 0 through a 4-step dialog
// chain (gated on 3x item 182201399) and flips to REWARD.
// Skip vs Java: SETPRO3 calls TeleportService2.teleportTo(player, 220020000, ...) to send the
// player to Heiron - no TeleportService2 exists in this port. The var/status transition to REWARD
// is kept (via ChangeQuestStepAsync, persisting both atomically) so the quest stays completable at
// the same NPC without the teleport.
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

public sealed class _1482ATeleportationAdventure : QuestHandlerBase
{
    private const int QuestIdConst = 1482;
    private const int OnesimusNpc  = 203919;
    private const int SonirimNpc   = 203337;
    private const int TokenItemId  = 182201399;

    public _1482ATeleportationAdventure(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(OnesimusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(OnesimusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SonirimNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == OnesimusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId != SonirimNpc) return false;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.QUEST_SELECT when var == 1:
                {
                    long itemCount = player.Inventory.FindByItemId(TokenItemId)?.Count ?? 0;
                    if (itemCount >= 3)
                    {
                        entry.SetVar(0, var + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
                }
                case DialogAction.QUEST_SELECT when var == 2:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                case DialogAction.SETPRO1:
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                case DialogAction.SETPRO3:
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
                    return true;
                default:
                    return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
