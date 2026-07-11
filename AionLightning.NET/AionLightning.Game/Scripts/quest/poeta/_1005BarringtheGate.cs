// Port of Java data/scripts/system/handlers/quest/poeta/_1005BarringtheGate.java (MrPoke/apozema).
// Campaign finale: 5-NPC talk chain (vars 0-4), then use the three power generators
// (700081/700082/700083 → vars 6/7/8) and the Abyss Gate (700080 → REWARD + movie 21),
// turn in at Kalio (movie 171 on USE_OBJECT).
// Parity notes: the generator "destroy" keeps Java's 100ms deferred still-targeting check;
// the destroy-emotion TODO in Java was never implemented there either.
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

namespace Quest.Poeta;

public sealed class _1005BarringtheGate : QuestHandlerBase
{
    private const int QuestIdConst = 1005;

    private static readonly int[] _talkNpcs = [203067, 203081, 790001, 203085, 203086, 700080, 700081, 700082, 700083];
    private static readonly int[] _precedingQuests = [1100, 1001, 1002, 1003, 1004];

    public _1005BarringtheGate(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int id in _talkNpcs)
            engine.RegisterQuestNpc(id).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, _precedingQuests, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _precedingQuests, isZoneMission: true, ct);

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
                case 203067: // Kalio
                    if (dialog == DialogAction.QUEST_SELECT && var == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (dialog == DialogAction.SETPRO1 && var == 0)
                        return await AdvanceAsync(conn, entry, targetObjId, var, ct);
                    return false;

                case 203081: // Oz
                    if (dialog == DialogAction.QUEST_SELECT && var == 1)
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (dialog == DialogAction.SETPRO2 && var == 1)
                        return await AdvanceAsync(conn, entry, targetObjId, var, ct);
                    return false;

                case 790001: // Pernos
                    if (dialog == DialogAction.QUEST_SELECT && var == 2)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.SETPRO3 && var == 2)
                        return await AdvanceAsync(conn, entry, targetObjId, var, ct);
                    return false;

                case 203085: // Poa
                    if (dialog == DialogAction.QUEST_SELECT && var == 3)
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (dialog == DialogAction.SETPRO4 && var == 3)
                        return await AdvanceAsync(conn, entry, targetObjId, var, ct);
                    return false;

                case 203086: // Ino
                    if (dialog == DialogAction.QUEST_SELECT && var == 4)
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (dialog == DialogAction.SETPRO5 && var == 4)
                        return await AdvanceAsync(conn, entry, targetObjId, var, ct);
                    return false;

                case 700081 when var == 5: Destroy(6, env, conn);  return false; // Green Power Generator
                case 700082 when var == 6: Destroy(7, env, conn);  return false; // Blue Power Generator
                case 700083 when var == 7: Destroy(8, env, conn);  return false; // Violet Power Generator
                case 700080 when var == 8: Destroy(-1, env, conn); return false; // Poeta Abyss Gate
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == 203067)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                await PlayQuestMovieAsync(conn, player, 171, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    private async ValueTask<bool> AdvanceAsync(GsClientConnection conn, QuestEntry entry, int targetObjId, int var, CancellationToken ct)
    {
        entry.SetVar(0, var + 1);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
    }

    // Java's private destroy(): 100ms deferred, only applies while the player still targets the object.
    private void Destroy(int nextVar, QuestEnv env, GsClientConnection conn)
    {
        int targetObjectId = env.Target?.ObjectId ?? 0;
        var player = env.Player;

        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            if (player.Target?.ObjectId != targetObjectId) return;
            var entry = player.Quests.Get(QuestId);
            if (entry is null) return;

            switch (nextVar)
            {
                case 6 or 7 or 8:
                    entry.SetVar(0, nextVar);
                    break;
                case -1:
                    await PlayQuestMovieAsync(conn, player, 21, CancellationToken.None);
                    entry.Status = QuestStatus.REWARD;
                    break;
            }
            await UpdateQuestStatusAsync(conn, entry, CancellationToken.None);
        });
    }
}
