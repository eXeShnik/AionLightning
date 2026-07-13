// Port of Java data/scripts/system/handlers/quest/aturam_sky_fortress/_18301MyPrec_H_ious.java (Cheatkiller).
// Talk to 799530 to start (plays movie 469 on accept); talk to 730373 seven times (SETPRO1: despawns
// the current target npc and spawns 700978 at its position, var0++); at var0==7 talk to 730374
// (SETPRO2: grants item 182212110, flips REWARD); turn in at 799530.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.AturamSkyFortress;

public sealed class _18301MyPrec_H_ious : QuestHandlerBase
{
    private const int QuestIdConst = 18301;
    private const int EndNpc       = 799530;
    private const int RepeatNpc    = 730373;
    private const int TurnInNpc    = 730374;
    private const int SpawnedNpc   = 700978;
    private const int RewardItem   = 182212110;

    private readonly IItemDao _itemDao;

    public _18301MyPrec_H_ious(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(EndNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RepeatNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == EndNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    await PlayQuestMovieAsync(conn, player, 469, ct);
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        if (entry is null) return false;

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == RepeatNpc && var < 7)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (env.Target is Npc npcTarget)
                    {
                        // Java bug: npc.getController().onDelete() (despawn) is skipped — no NPC
                        // AI/controller subsystem in this port (matches gelkmaros/_21105CoweringRefugee.cs).
                        var pos = npcTarget.Position;
                        SpawnQuestNpc(pos.WorldId, pos.InstanceId, SpawnedNpc, pos.X, pos.Y, pos.Z, 0);
                        await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    }
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            else if (targetId == TurnInNpc && var == 7)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, RewardItem, 1, ct);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == EndNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
