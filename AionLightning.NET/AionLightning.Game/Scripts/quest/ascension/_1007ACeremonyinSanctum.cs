// Port of Java data/scripts/system/handlers/quest/ascension/_1007ACeremonyinSanctum.java.
// Chain: Pernos (790001, var0->1) -> Leah (203725, var1->2, movie 92) -> Jucleas (203752, var2 ->
// class-keyed var 10/20/30/40/50/60 + REWARD, movie 91) -> the matching starting-class trainer
// (203758 warrior / 203759 scout / 203760 mage / 203761 priest / 801212 engineer / 801213 artist)
// grants the reward.
//
// Skip vs Java: Pernos' SETPRO1 teleports the player to 110010000 via TeleportService2, which
// isn't ported in this port; the var advance + close-dialog is kept so the chain stays completable
// without the teleport. OnLevelUpAsync's CraftSkillUpdateService.setMorphRecipe call (crafting
// recipe unlock tied to class change) is skipped — no craft-recipe-unlock service exists yet.
// This quest's own start precondition is quest 1006 ("Ascension"), which is DEFERRED (instance-
// scale: private-instance combat encounter + ClassChangeService, neither ported) — so under the
// current migration state this quest's entry is never created via the level-up chain. The
// OnLevelUpAsync wiring is kept faithful to Java (defaultOnLvlUpEvent(env, 1006)) so it starts
// correctly once 1006 is ported or the entry is created by other means (e.g. test/GM tooling).
//
// Java bug fixed: the dialog switch nests `case QUEST_SELECT: {...} case SETPROn: {...}` with no
// break between them, so if a player re-targets an NPC after already advancing past its gate var,
// the QUEST_SELECT branch falls through into the SETPROn branch unconditionally (re-running the
// step transition). This port evaluates each dialog action independently instead of relying on
// that fallthrough, matching the convention already used by other ported zones (e.g.
// eltnen/_1482ATeleportationAdventure, ishalgen/_2007WheresRaeThisTime).
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

public sealed class _1007ACeremonyinSanctum : QuestHandlerBase
{
    private const int QuestIdConst = 1007;
    private const int PernosNpc    = 790001;
    private const int LeahNpc      = 203725;
    private const int JucleasNpc   = 203752;

    // trainer npc id -> (required var, USE_OBJECT intro page, SELECT_QUEST_REWARD confirm page, reward index)
    private static readonly Dictionary<int, (int RequiredVar, int IntroPage, int RewardPage, int RewardIndex)> _trainers = new()
    {
        [203758] = (10, 2034, 5, 0),
        [203759] = (20, 2375, 6, 0),
        [203760] = (30, 2716, 7, 0),
        [203761] = (40, 3057, 8, 0),
        [801212] = (50, 3398, 45, 0),
        [801213] = (60, 3739, 46, 0),
    };

    public _1007ACeremonyinSanctum(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(PernosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LeahNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JucleasNpc).OnTalk.Add(QuestId);
        foreach (int npcId in _trainers.Keys)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1006, ct);

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
            if (targetId == PernosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    // Skip: Java teleports to 110010000 here (TeleportService2 not ported) - see header.
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == LeahNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_ACTION_1353)
                {
                    await PlayQuestMovieAsync(conn, player, 92, ct);
                    return true;
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            if (targetId == JucleasNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SELECT_ACTION_1694)
                {
                    await PlayQuestMovieAsync(conn, player, 91, ct);
                    return true;
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
                        _                    => var, // unreachable: GetStartingClassFor always yields one of the above (Java's dead default branch, kept faithfully)
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
