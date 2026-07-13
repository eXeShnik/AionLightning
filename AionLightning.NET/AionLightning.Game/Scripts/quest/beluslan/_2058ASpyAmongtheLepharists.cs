// Port of Java data/scripts/system/handlers/quest/beluslan/_2058ASpyAmongtheLepharists.java
// (Hellboy/vlog). Talk to Tristran (204774, gives disguise 164000233 + movie 249, var0 0->1);
// Stua (204809, gives Signal Flare 182204317, starts a 240s timer, var0 1->2); use the Secret Port
// Entrance (700359, var0==2) to play movie 250; on movie 250 end advance var0 2->3; kill the Research
// Center Power Generator (700349, var0 3->4); use the Signal Flare (182204317) to finish (movie 251,
// REWARD); report to Tristran.
// Skips vs Java (cosmetic / unported, state transitions preserved):
//   - SkillEngine disguise effect 1865 (apply/remove) - no quest skill-effect gating ported.
//   - TeleportService2 relocation on movie 250 end - progression is the var 2->3 step, kept.
//   - the DF3_ITEMUSEAREA_Q2058 sub-zone restriction on the flare use - no in-item-use zone API.
//   - registerOnDie / registerOnLogOut failure-resets - no OnDie/OnLogOut hooks in this port.
//   - the QUEST_FAILED system message on the enter-world reset - cosmetic notice.
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

public sealed class _2058ASpyAmongtheLepharists : QuestHandlerBase
{
    private const int QuestIdConst = 2058;
    private const int Tristran     = 204774;
    private const int Stua         = 204809;
    private const int SecretPortEntrance = 700359;
    private const int PowerGenerator = 700349;
    private const int DisguiseItem = 164000233;
    private const int SignalFlare  = 182204317;
    private const int PortWorldId  = 320110000;
    private const int MovieId      = 250;

    private readonly IItemDao _itemDao;

    public _2058ASpyAmongtheLepharists(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(SignalFlare, QuestId);
        engine.RegisterOnQuestMovieEnd(MovieId, QuestId);
        engine.RegisterQuestNpc(Tristran).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Stua).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecretPortEntrance).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PowerGenerator).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Tristran)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status != QuestStatus.START) return false;

        if (targetId == Tristran)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            // Java switch fallthrough: QUEST_SELECT falls into SETPRO1
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, DisguiseItem, 1, ct);
                await PlayQuestMovieAsync(conn, player, 249, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            return false;
        }
        if (targetId == Stua)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            // Java switch fallthrough: QUEST_SELECT falls into SETPRO2
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO2)
            {
                if (var != 1) return false;
                if (!await GiveQuestItemAsync(player, conn, _itemDao, SignalFlare, 1, ct))
                    return false;
                StartQuestTimer(env, conn, 240);
                // note: Java applies disguise effect 1865 here (350s) - cosmetic, dropped.
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            }
            return false;
        }
        if (targetId == SecretPortEntrance && var == 2)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                // note: Java ends the timer + removes disguise effect 1865 here; the timer-end guard
                // (var == 2) no longer matches once movie 250 advances var to 3, so it is harmless.
                await PlayQuestMovieAsync(conn, player, 250, ct);
                return true;
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != MovieId) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        // note: Java relocates the player via TeleportService2 here - cosmetic, the var 2->3 step is the progression.
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        return true;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, PowerGenerator, 3, 4, ct);

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != SignalFlare) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 4) return false;

        // note: Java gates this on the DF3_ITEMUSEAREA_Q2058 sub-zone - no in-item-use zone API ported, so dropped.
        await RemoveQuestItemAsync(player, conn, _itemDao, SignalFlare, 1, ct);
        await PlayQuestMovieAsync(conn, player, 251, ct);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (player.Position.WorldId != PortWorldId && entry.GetVar(0) == 3)
        {
            entry.SetVar(0, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (entry.GetVar(0) == 2)
        {
            entry.SetVar(0, 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2500, isZoneMission: true, ct);
}
