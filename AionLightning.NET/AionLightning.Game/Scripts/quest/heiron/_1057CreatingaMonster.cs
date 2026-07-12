// Port of Java data/scripts/system/handlers/quest/heiron/_1057CreatingaMonster.java.
// Talk to 204502 (var0->1) then 204619 (var1->2); use object 700218 to obtain item 182201616
// (var2->3), hand it to 204502 (var3->4); entering world 310050000 bumps to var5, then killing
// 700219 three times (var5->8) and 212211 once (var8->9) unlocks object 700279 for the reward.
// Mission-chain quest (no NPC quest-offer dialog), gated on 1500 and 1056.
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

public sealed class _1057CreatingaMonster : QuestHandlerBase
{
    private const int QuestIdConst = 1057;
    private const int FirstNpc  = 204502;
    private const int SecondNpc = 204619;
    private const int ObjectNpc = 700218;
    private const int FinalObjectNpc = 700279;
    private const int EndNpc    = 204500;
    private const int GrowthItem = 182201616;
    private const int KillNpcOne = 700219;
    private const int KillNpcTwo = 212211;
    private const int TargetWorldId = 310050000;

    private readonly IItemDao _itemDao;

    public _1057CreatingaMonster(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(KillNpcOne).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(KillNpcTwo).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FinalObjectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, 1056, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, [1500, 1056], isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == EndNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (targetId == FirstNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                return false;
            }
            if (dialog == DialogAction.SELECT_ACTION_2036)
            {
                if (var == 3) await PlayQuestMovieAsync(conn, player, 190, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO4 && var == 3)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, reward: false, sameNpc: false,
                    giveItemId: 0, giveItemCount: 0, removeItemId: GrowthItem, removeItemCount: 1, ct);
        }
        else if (targetId == SecondNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
        }
        else if (targetId == ObjectNpc && var == 2)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3)
                return await UseQuestObjectAsync(env, conn, 2, 3, false, 0, GrowthItem, 1, 0, 0, 0, false, _itemDao, ct);
        }
        else if (targetId == FinalObjectNpc && var == 9)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await UseQuestObjectAsync(env, conn, 9, 9, true, false, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START
            && env.Player.Position.WorldId == TargetWorldId && entry.GetVar(0) == 4)
        {
            entry.SetVar(0, 5);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (env.TargetId == KillNpcOne && entry.GetVar(0) < 8)
        {
            entry.SetVar(0, entry.GetVar(0) + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        else if (env.TargetId == KillNpcTwo && entry.GetVar(0) == 8)
        {
            entry.SetVar(0, entry.GetVar(0) + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false;
    }
}
