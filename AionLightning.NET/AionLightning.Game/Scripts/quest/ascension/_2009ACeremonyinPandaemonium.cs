// Port of Java data/scripts/system/handlers/quest/ascension/_2009ACeremonyinPandaemonium.java.
// Chain: 203550 (var0->1) -> 204182 (var1->2, movie 121) -> 204075 (var2 -> class-keyed var
// 10/20/30/40/50/60 + REWARD, movie 122) -> the matching starting-class trainer (204080 warrior /
// 204081 scout / 204082 mage / 204083 priest / 801220 engineer / 801221 artist) grants the reward,
// each at its own reward-index tier (0..5).
//
// Skip vs Java: 203550's SETPRO1 teleports the player to 120010000 via TeleportService2, which
// isn't ported; the var advance + close-dialog is kept so the chain stays completable without the
// teleport. OnLevelUpAsync's CraftSkillUpdateService.setMorphRecipe call (crafting recipe unlock
// tied to class change) is skipped — no craft-recipe-unlock service exists yet. This quest's own
// start precondition is quest 2008 ("Ascension", Asmodian), which is DEFERRED (instance-scale:
// private-instance combat encounter + ClassChangeService, neither ported) — so under the current
// migration state this quest's entry is never created via the level-up chain. OnLevelUpAsync is
// kept faithful to Java (defaultOnLvlUpEvent(env, 2008)) so it starts correctly once 2008 is
// ported or the entry is created by other means (e.g. test/GM tooling).
using System.Collections.Generic;
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

namespace Quest.Ascension;

public sealed class _2009ACeremonyinPandaemonium : QuestHandlerBase
{
    private const int QuestIdConst = 2009;
    private const int StartNpc     = 203550;
    private const int SecondNpc    = 204182;
    private const int ThirdNpc     = 204075;

    // trainer npc id -> (required var, USE_OBJECT intro page, SELECT_QUEST_REWARD confirm page, reward index)
    private static readonly Dictionary<int, (int RequiredVar, int IntroPage, int RewardPage, int RewardIndex)> _trainers = new()
    {
        [204080] = (10, 2034, 5, 0),
        [204081] = (20, 2375, 6, 1),
        [204082] = (30, 2716, 7, 2),
        [204083] = (40, 3057, 8, 3),
        [801220] = (50, 3398, 45, 4),
        [801221] = (60, 3739, 46, 5),
    };

    public _2009ACeremonyinPandaemonium(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
        foreach (int npcId in _trainers.Keys)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2008, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    // Skip: Java teleports to 120010000 here (TeleportService2 not ported) - see header.
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_ACTION_1353 && var == 1)
                {
                    await PlayQuestMovieAsync(conn, player, 121, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            if (targetId == ThirdNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SELECT_ACTION_1694 && var == 2)
                {
                    await PlayQuestMovieAsync(conn, player, 122, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO3 && var == 2)
                {
                    int newVar = player.PlayerClass.GetStartingClassFor() switch
                    {
                        PlayerClass.WARRIOR  => 10,
                        PlayerClass.SCOUT    => 20,
                        PlayerClass.MAGE     => 30,
                        PlayerClass.PRIEST   => 40,
                        PlayerClass.ENGINEER => 50,
                        PlayerClass.ARTIST   => 60,
                        _                    => var, // unreachable: GetStartingClassFor always yields one of the above
                    };
                    entry.SetVar(0, newVar);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (!_trainers.TryGetValue(targetId, out var t) || var != t.RequiredVar) return false;

            int dialogId = env.DialogId;
            if (dialogId == (int)DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, t.IntroPage, ct);
            if (dialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, t.RewardPage, ct);
            if (dialogId >= (int)DialogAction.SELECTED_QUEST_REWARD1 && dialogId <= (int)DialogAction.SELECTED_QUEST_REWARD11)
            {
                if (await FinishQuestAsync(conn, player, t.RewardIndex, ct))
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        return false;
    }
}
