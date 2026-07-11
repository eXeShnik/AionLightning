using AionLightning.Commons.Events;
using AionLightning.Game.Events;
using AionLightning.Game.Network.Aion;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;

namespace AionLightning.Game.QuestEngine;

/// <summary>
/// Bridges <see cref="PlayerEnteredWorldEvent"/> (published by <c>CM_LEVEL_READY</c> once the
/// client has finished loading) into <see cref="QuestEngine.OnEnterWorldAsync"/> — mirrors
/// <c>Combat.Handlers.PvpKillHandler</c>'s event-bus-to-engine bridging pattern. The event only
/// carries the <see cref="Model.Player"/>, so the connection is looked up via
/// <see cref="PlayerConnectionRegistry"/>.
/// </summary>
public sealed class QuestEnterWorldHandler(PlayerConnectionRegistry connRegistry, QuestEngineType questEngine)
    : IEventHandler<PlayerEnteredWorldEvent>
{
    public async ValueTask HandleAsync(PlayerEnteredWorldEvent e, CancellationToken ct)
    {
        var conn = connRegistry.Get(e.Player.ObjectId);
        if (conn is null) return;

        await questEngine.OnEnterWorldAsync(e.Player, conn, ct);
    }
}
