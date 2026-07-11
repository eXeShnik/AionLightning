// Port of Java data/scripts/system/handlers/quest/eltnen/_1468HannetsLostLove.java (MrPoke, remod).
// Talk to Hannet (790004) to start and to finish; a 3-npc relay chain (203184 -> 204007 -> 203969)
// advances var 0 from 0 to 3 before the reward turn-in.
// Skip vs Java: the SELECT_QUEST_REWARD branch calls sendEmotion(env, player, EmotionId.STAND,
// true) - a cosmetic sit/stand cue with no state effect; omitted (no equivalent helper), quest
// still completes identically.
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

namespace Quest.Eltnen;

public sealed class _1468HannetsLostLove : QuestHandlerBase
{
    private const int QuestIdConst = 1468;
    private const int HannetNpc    = 790004;
    private const int SecondNpc    = 203184;
    private const int ThirdNpc     = 204007;
    private const int FourthNpc    = 203969;

    public _1468HannetsLostLove(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(HannetNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(HannetNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FourthNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        int targetId = env.TargetId;
        var entry = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == HannetNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.SetVar(0, 3);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        if (entry is not { Status: QuestStatus.START }) return false;

        if (targetId == SecondNpc && entry.GetVar(0) == 0)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }
        if (targetId == ThirdNpc && entry.GetVar(0) == 1)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO2)
            {
                entry.SetVar(0, 2);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }
        if (targetId == FourthNpc && entry.GetVar(0) == 2)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO3)
            {
                entry.SetVar(0, 3);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }
        return false;
    }
}
