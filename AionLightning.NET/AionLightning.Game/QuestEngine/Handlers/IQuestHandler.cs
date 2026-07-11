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

    ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    ValueTask<bool> OnSkillUseAsync(Player player, int skillId, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>PvP Phase 1 (Java onKillInWorldEvent): a registered kill_in_world quest's world was the scene of a player kill.</summary>
    ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) => ValueTask.FromResult(false);
}
