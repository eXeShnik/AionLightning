// Port of Java data/scripts/system/handlers/quest/beluslan/_2057GlacionttheHardy.java.
// Chieftain Akagitan (204787, var0->1, movie 246), Delris (204784, var1->2, movie 247, gives the
// Fire Bomb 182204316); using the bomb inside DF3_ITEMUSEAREA_Q2057 (var2->3, movie 248) opens the
// hunt: killing any of the 5 Ice Petrahulks marks its own var slot (1-5) unless it's the last one
// standing, in which case the kill flips straight to REWARD instead.
using System.Linq;
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

namespace Quest.Beluslan;

public sealed class _2057GlacionttheHardy : QuestHandlerBase
{
    private const int QuestIdConst = 2057;
    private const int ChieftainNpc = 204787;
    private const int DelrisNpc = 204784;
    private const int FireBombItem = 182204316;
    private const string ItemUseZone = "DF3_ITEMUSEAREA_Q2057";

    private static readonly (int NpcId, int VarSlot)[] _bosses =
    [
        (213730, 1), (213788, 2), (213789, 3), (213790, 4), (213791, 5)
    ];

    private readonly IItemDao _itemDao;

    public _2057GlacionttheHardy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(FireBombItem, QuestId);
        foreach (var (npcId, _) in _bosses)
            engine.RegisterQuestNpc(npcId).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(ChieftainNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DelrisNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 2056, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, [2500, 2056], isZoneMission: true, ct);

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
            if (targetId == ChieftainNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == ChieftainNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SELECT_ACTION_1012)
            {
                await PlayQuestMovieAsync(conn, player, 246, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
            }
            if (dialog == DialogAction.SETPRO1)
            {
                await PlayQuestMovieAsync(conn, player, 246, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            return false;
        }
        if (targetId == DelrisNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2)
            {
                await PlayQuestMovieAsync(conn, player, 247, ct);
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                    giveItemId: FireBombItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            }
            return false;
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;

        foreach (var (npcId, slot) in _bosses)
        {
            if (env.TargetId != npcId || entry.GetVar(slot) != 0) continue;

            int alreadyDead = _bosses.Count(b => entry.GetVar(b.VarSlot) == 1);
            if (alreadyDead == 4)
                entry.Status = QuestStatus.REWARD;
            else
                entry.SetVar(slot, 1);

            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != FireBombItem) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 2) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, FireBombItem, 1, ct);
        await PlayQuestMovieAsync(conn, player, 248, ct);
        entry.SetVar(0, 3);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
