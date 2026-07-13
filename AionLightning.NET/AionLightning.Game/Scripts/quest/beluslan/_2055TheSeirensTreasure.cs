// Port of Java data/scripts/system/handlers/quest/beluslan/_2055TheSeirensTreasure.java.
// Sleipnir (204768, var0->1 gives 182204310 + movie 239, var6->reward), Rubelik (204743, var1->2
// swaps 182204310 for 182204311), Esnu (204808, var2->3, var3->4 movie 240 removes 182204311,
// collect-item check var4->5, var5->6 gives the temple key 182204321). Skip vs Java: the
// FINISH_DIALOG branch's discarded defaultCloseDialog(5,5)/(4,4) side call (Java bug — always
// falls through to the SETPRO6 (5,6) transition regardless of its own result) is collapsed into
// that one real transition.
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

namespace Quest.Beluslan;

public sealed class _2055TheSeirensTreasure : QuestHandlerBase
{
    private const int QuestIdConst = 2055;
    private const int SleipnirNpc = 204768;
    private const int RubelikNpc = 204743;
    private const int EsnuNpc = 204808;
    private const int LetterItem = 182204310;
    private const int ReplyItem = 182204311;
    private const int TempleKeyItem = 182204321;

    private static readonly int[] _lvlUpQuests = [2500, 2054];

    private readonly IItemDao _itemDao;

    public _2055TheSeirensTreasure(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(SleipnirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RubelikNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EsnuNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 2054, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _lvlUpQuests, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == SleipnirNpc) return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == SleipnirNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 6)
                {
                    var ringItem = player.Inventory.FindByItemId(TempleKeyItem);
                    if (ringItem is not null && ringItem.Count >= 1)
                        return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                }
                return false;
            }
            if (dialog == DialogAction.SETPRO1 && var == 0)
            {
                await PlayQuestMovieAsync(conn, player, 239, ct);
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                    giveItemId: LetterItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            }
            if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD && var == 6)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, TempleKeyItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: true, ct);
            }
            if (dialog == DialogAction.SELECT_ACTION_3143) return await SendQuestDialogAsync(conn, targetObjId, 3143, ct);
            if (dialog == DialogAction.SETPRO7 && var == 6)
            {
                await PlayQuestMovieAsync(conn, player, 239, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, TempleKeyItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: true, ct);
            }
            return false;
        }
        if (targetId == RubelikNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                    giveItemId: ReplyItem, giveItemCount: 1, removeItemId: LetterItem, removeItemCount: 1, ct);
            return false;
        }
        if (targetId == EsnuNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO4 && var == 3)
            {
                await PlayQuestMovieAsync(conn, player, 240, ct);
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, reward: false, sameNpc: false,
                    giveItemId: 0, giveItemCount: 0, removeItemId: ReplyItem, removeItemCount: 1, ct);
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 5, false, 10000, 10001, ct);
            if (dialog == DialogAction.FINISH_DIALOG || dialog == DialogAction.SETPRO6)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 6, reward: false, sameNpc: false,
                    giveItemId: TempleKeyItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            return false;
        }
        return false;
    }
}
