// Port of Java data/scripts/system/handlers/quest/ishalgen/_2002WheresRae.java.
// Nobekk (203519, var0->1) -> Dabi (203534, movie 52, var1->2) -> Verdandi (790002, var2->3) ->
// kill 210377/210378 x7 (var3->10) -> Verdandi item-check (var11: Sticky Mushroom 700045 grants
// collect item 182203003, var11->12) -> Verdandi (var12->13, dungeon excursion collapsed, see
// below) -> Verdandi (var13->14) -> Cute Ribbit (203538, var14->15, spawns Rae 203553) -> Rae
// turn-in (var15 -> REWARD) -> Ulgorn (203516) grants the reward.
//
// Skips vs Java (documented per the port's "never strand a quest var" rule):
// - Java's own register() npc list omits Verdandi (790002) even though her dialog switch is the
//   *only* path advancing var 2 through 13 — without registering her OnTalk here too, the quest
//   would be permanently stuck after var 2. Treated as a Java registration bug and fixed, not
//   ported literally (mirrors the "never leave a quest var stuck" guidance).
// - Java's SETPRO5 (var==12) branch sets a var==99 sentinel, closes the dialog, and teleports the
//   player into instance zone 320010000 (InstanceService + TeleportService2, neither ported) to
//   presumably complete a mini-dungeon; the exit path is a scheduled flight-teleport at Hagen
//   (205020) 40s later that flips var 99->13 — but Hagen is *also* never registered OnTalk in
//   Java, so that exit path is unreachable there too. Collapsed the whole excursion into a direct
//   var 12->13 step (same precedent as Poeta _1002RequestoftheElim's instance-teleport collapse);
//   Hagen's branch is omitted entirely since it becomes dead code once the excursion is collapsed.
// - Java's Sticky Mushroom (700045) USE_OBJECT applies a "harvest" skill effect
//   (SkillEngine.applyEffectDirectly(8343, ...)) that indirectly rolls the quest_data.xml
//   quest_drop for collect item 182203003 (collecting_step=11). No SkillEngine/skill-effect infra
//   exists in this port, so the collect item is granted directly instead — same player-facing
//   result (the item needed for the CHECK_USER_HAS_QUEST_ITEM gate).
// - Java despawns the Cute Ribbit npc (getController().onDie) after spawning Rae, and despawns Rae
//   herself on turn-in (getController().onDelete()) — both are cosmetic NPC-controller calls with
//   no equivalent in this port (no NPC AI/controller subsystem yet) and are omitted; the movie 256
//   call after the Ribbit interaction was already commented out in the Java source itself.
using System.Collections.Generic;
using System.Linq;
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

namespace Quest.Ishalgen;

public sealed class _2002WheresRae : QuestHandlerBase
{
    private const int QuestIdConst      = 2002;
    private const int NobekkNpc         = 203519;
    private const int DabiNpc           = 203534;
    private const int VerdandiNpc       = 790002;
    private const int StickyMushroomObj = 700045;
    private const int CuteRibbitNpc     = 203538;
    private const int RaeNpc            = 203553;
    private const int UlgornNpc         = 203516;
    private const int MushroomItemId    = 182203003;

    private static readonly int[] _mobs = [210377, 210378];

    private readonly IItemDao _itemDao;

    public _2002WheresRae(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(NobekkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DabiNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VerdandiNpc).OnTalk.Add(QuestId); // deviation: see header (Java omits this registration)
        engine.RegisterQuestNpc(StickyMushroomObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CuteRibbitNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RaeNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UlgornNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2100, isZoneMission: true, ct);

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
            switch (targetId)
            {
                case NobekkNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            return var == 0 ? await SendQuestDialogAsync(conn, targetObjId, 1011, ct) : false;
                        case DialogAction.SETPRO1:
                            return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                        default:
                            return false;
                    }

                case DabiNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            return var == 1 ? await SendQuestDialogAsync(conn, targetObjId, 1352, ct) : false;
                        case DialogAction.SELECT_ACTION_1353:
                            await PlayQuestMovieAsync(conn, player, 52, ct);
                            return false;
                        case DialogAction.SETPRO2:
                            return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                        default:
                            return false;
                    }

                case VerdandiNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            return var switch
                            {
                                2  => await SendQuestDialogAsync(conn, targetObjId, 1693, ct),
                                10 => await SendQuestDialogAsync(conn, targetObjId, 2034, ct),
                                11 => await SendQuestDialogAsync(conn, targetObjId, 2375, ct),
                                12 => await SendQuestDialogAsync(conn, targetObjId, 2462, ct),
                                13 => await SendQuestDialogAsync(conn, targetObjId, 2716, ct),
                                _  => false,
                            };
                        case DialogAction.SETPRO3:
                        case DialogAction.SETPRO4:
                        case DialogAction.SETPRO6:
                            if (var == 2 || var == 10)
                                return await DefaultCloseDialogAsync(env, conn, var, var + 1, ct);
                            if (var == 13)
                                return await DefaultCloseDialogAsync(env, conn, 13, 14, ct);
                            return false;
                        case DialogAction.SETPRO5:
                            if (var == 12 || var == 99)
                            {
                                // Collapsed dungeon excursion (var 12 -> 13 directly) — see header.
                                entry.SetVar(0, 13);
                                await UpdateQuestStatusAsync(conn, entry, ct);
                                return await CloseDialogWindowAsync(conn, targetObjId, ct);
                            }
                            return false;
                        case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                            return var == 11
                                && await CheckQuestItemsAsync(env, conn, _itemDao, 11, 12, false, 2461, 2376, ct);
                        default:
                            return false;
                    }

                case StickyMushroomObj:
                    if (var == 11 && dialog == DialogAction.USE_OBJECT)
                    {
                        // Skip: no SkillEngine/harvest-effect infra — grant the collect item directly.
                        await GiveQuestItemAsync(player, conn, _itemDao, MushroomItemId, 1, ct);
                        return true;
                    }
                    return false;

                case CuteRibbitNpc:
                    if (var == 14 && dialog == DialogAction.USE_OBJECT && env.Target is not null)
                    {
                        var pos = env.Target.Position;
                        bool result = await DefaultCloseDialogAsync(env, conn, 14, 15, ct);
                        SpawnQuestNpc(pos.WorldId, pos.InstanceId, RaeNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                        return result;
                    }
                    return false;

                case RaeNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            return var == 15 ? await SendQuestDialogAsync(conn, targetObjId, 3057, ct) : false;
                        case DialogAction.SETPRO7:
                            if (var != 15) return false;
                            entry.Status = QuestStatus.REWARD;
                            await UpdateQuestStatusAsync(conn, entry, ct);
                            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                        default:
                            return false;
                    }

                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == UlgornNpc)
        {
            if (dialog is DialogAction.USE_OBJECT or DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
            if (dialog == DialogAction.SETPRO8)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_mobs, 3, 10, ct);
}
