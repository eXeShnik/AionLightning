using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine.Model;

namespace AionLightning.Game.QuestEngine.Handlers;

/// <summary>
/// A data-driven or hand-written quest handler (Java <c>questEngine.handlers.QuestHandler</c> port).
/// Event methods default to no-op/unhandled so a handler only needs to override what it uses.
/// </summary>
public interface IQuestHandler
{
    int QuestId { get; }

    /// <summary>Registers this handler's NPC/item indexes into <paramref name="engine"/>.</summary>
    void Register(QuestEngine engine);

    ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    ValueTask<bool> OnAttackAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    ValueTask<bool> OnSkillUseAsync(Player player, int skillId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>PvP Phase 1 (Java onKillInWorldEvent): a registered kill_in_world quest's world was the scene of a player kill.</summary>
    ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>Java onLvlUpEvent: the player leveled up; re-check this quest's mission-start preconditions.</summary>
    ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>Java onZoneMissionEndEvent: a related zone-mission quest was just turned in; re-check this quest's mission-start preconditions.</summary>
    ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>Java onMovieEndEvent: a cutscene registered against this quest finished playing.</summary>
    ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>Java onEnterWorldEvent: the player just finished loading into the world (login or zone-in).</summary>
    ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>Java onEnterZoneEvent: the player just entered a named zone region registered against this quest.</summary>
    ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>Java onDieEvent: the player died; quests registered via RegisterOnDie may reset/fail a step.</summary>
    ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>Java onLogOutEvent: the player logged out; conn may be null (fired during teardown).</summary>
    ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct) => ValueTask.FromResult(false);
}
