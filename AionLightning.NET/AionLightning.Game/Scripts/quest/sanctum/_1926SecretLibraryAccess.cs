// Port of Java data/scripts/system/handlers/quest/sanctum/_1926SecretLibraryAccess.java (xaerolt, Rolandas).
// Talk to 203894 to start; 203098 grants item 182206022 and flips to REWARD (its QUEST_SELECT
// dialog text differs only cosmetically depending on whether quests 1020/14016 are complete);
// return to 203894 to finish (removes the item). Skip: Java's post-COMPLETE branches at 203894
// and npc 203895 use TeleportService2 to warp the player into the library as a convenience --
// no teleport service exists in this port, so those decorative branches are omitted; the quest
// itself is fully completable without them (npc 203895 is intentionally not registered here since
// it has no other behavior).
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

namespace Quest.Sanctum;

public sealed class _1926SecretLibraryAccess : QuestHandlerBase
{
    private const int QuestIdConst    = 1926;
    private const int StartNpc        = 203894;
    private const int PreconditionNpc = 203098;
    private const int LibraryItemId   = 182206022;
    private const int VerteronQuestId = 1020;
    private const int AethertechQuestId = 14016;

    private readonly IItemDao _itemDao;

    public _1926SecretLibraryAccess(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PreconditionNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }

            if (entry.Status == QuestStatus.REWARD && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECTED_QUEST_NOREWARD)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, LibraryItemId, 1, ct);
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    if (!await FinishQuestAsync(conn, player, 0, ct)) return false;
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (targetId == PreconditionNpc)
        {
            if (entry is { Status: QuestStatus.START } && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (IsQuestComplete(player, VerteronQuestId) || IsQuestComplete(player, AethertechQuestId))
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1097, ct);
                }
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, LibraryItemId, 1, ct))
                    {
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                    }
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 0), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        return false;
    }

    private static bool IsQuestComplete(Player player, int questId)
    {
        var qs = player.Quests.Get(questId);
        return qs is not null && qs.Status == QuestStatus.COMPLETE;
    }
}
