using AionLightning.Commons.Events;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// Fires the quest player-death hook (Java <c>onDieEvent</c>) when a player dies, for quests that
/// registered via <c>RegisterOnDie</c>. No-op for NPC deaths.
/// </summary>
public sealed class QuestPlayerDeathHandler(QuestEngineType questEngine, PlayerConnectionRegistry connRegistry)
    : IEventHandler<DeathEvent>
{
    public async ValueTask HandleAsync(DeathEvent e, CancellationToken ct)
    {
        if (e.Victim is not Player player) return;
        var conn = connRegistry.Get(player.ObjectId);
        if (conn is null) return;
        await questEngine.OnDieAsync(player, conn, ct);
    }
}
