// Hand-written quest script (Phase 5 Batch 0 golden exemplar). Port of Java
// data/scripts/system/handlers/quest/poeta/_1000Prologue.java.
//
// Elyos-only prologue: starts automatically the first time the player enters the world, plays the
// intro cinematic (movie id 1), and completes as soon as that movie finishes. No NPC dialog.
//
// Script-authoring convention (see QuestEngineHostedService.LoadHandWrittenScripts): subclass
// QuestHandlerBase, hardcode the quest id as a compile-time constant passed to the base
// constructor, and expose exactly one public constructor shaped
// (IDataManager, IQuestDao, QuestRewardService, IItemDao) — the batch-compile host resolves and
// invokes that fixed constructor by reflection. This script doesn't touch items, so IItemDao is
// declared and simply unused.
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

namespace Quest.Poeta;

public sealed class _1000Prologue : QuestHandlerBase
{
    private const int QuestIdConst = 1000;

    public _1000Prologue(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnQuestMovieEnd(1, QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (player.Race != Race.ELYOS) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null)
        {
            entry = new QuestEntry { QuestId = QuestId, Status = QuestStatus.START };
            player.Quests.Add(entry);
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
            await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
                SM_QUEST_ACTION.ActionType.Accept, (byte)entry.Status, entry.Step), ct);
        }

        if (entry.Status != QuestStatus.START) return false;

        await conn.SendAsync(new SM_PLAY_MOVIE(1, 1), ct);
        return true;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != 1) return false;

        var player = env.Player;
        if (player.Race != Race.ELYOS) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        var template = Template;
        if (template is null) return false;

        return await RewardService.GrantAndCompleteAsync(conn, player, entry, template, rewardIndex: 0, ct);
    }
}
