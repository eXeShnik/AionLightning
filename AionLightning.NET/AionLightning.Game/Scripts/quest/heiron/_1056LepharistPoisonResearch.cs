// Port of Java data/scripts/system/handlers/quest/heiron/_1056LepharistPoisonResearch.java.
// Talk to 204504 (var0->1), then 204574 (var1->2); at 203705 hand in item 182201614 (var2->4,
// skipping 3), kill 212151 to reach var5 (status REWARD set explicitly), turn in at 203707.
// Mission-chain quest (no NPC quest-offer dialog), gated on 1500.
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

public sealed class _1056LepharistPoisonResearch : QuestHandlerBase
{
    private const int QuestIdConst = 1056;
    private const int FirstNpc  = 204504;
    private const int SecondNpc = 204574;
    private const int ThirdNpc  = 203705;
    private const int EndNpc    = 203707;
    private const int KillNpc   = 212151;
    private const int VialItem  = 182201614;

    private readonly IItemDao _itemDao;

    public _1056LepharistPoisonResearch(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1500, isZoneMission: true, ct);

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
            if (targetId == EndNpc) return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (targetId == FirstNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
        }
        else if (targetId == SecondNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
        }
        else if (targetId == ThirdNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                return false;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
            {
                if (var == 2 && player.Inventory.FindByItemId(VialItem) is { Count: 1 })
                {
                    await PlayQuestMovieAsync(conn, player, 101, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
            }
            if (dialog == DialogAction.SETPRO4 && var == 2)
            {
                entry.SetVar(0, 4);
                await RemoveQuestItemAsync(player, conn, _itemDao, VialItem, 1, ct);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            if (dialog == DialogAction.SET_SUCCEED && var == 5)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (env.TargetId == KillNpc && entry.GetVar(0) == 4)
        {
            entry.SetVar(0, 5);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false;
    }
}
