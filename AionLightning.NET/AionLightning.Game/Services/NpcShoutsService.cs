using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Services;

/// <summary>
/// Broadcasts NPC chat/shout messages to nearby players (Java <c>NpcShoutsService.sendMsg</c>).
/// AI scripts under <c>Scripts/ai/**</c> reach this through <see cref="Ai.NpcAi2"/>'s static
/// injection (see <see cref="Ai.NpcAi2.SendMsg"/>) — this is what lights up the ~90 <c>SendMsg(id)</c>
/// call sites already ported into those scripts (spawn/aggro/enrage/death flavor text keyed by
/// explicit string ids), which were no-ops until this service existed.
///
/// The data-driven shout table (npc_shouts.xml, looked up per NPC+event via
/// <see cref="DataHolders.NpcShoutData.GetRandomShout"/>) is a separate concern already wired inline
/// at the exact spots those events fire — SEE/ATTACK_BEGIN/ATTACK/ATTACK_END/IDLE in
/// <see cref="NpcAiService"/> and DIED in the NPC-kill branches of CM_ATTACK/CM_CASTSPELL. This
/// service does not duplicate or replace that path; it only adds the explicit-id broadcast scripts
/// call directly (Java's <c>sendMsg(npc, msg)</c> overload, delay=0/color=25/isShout=false).
/// </summary>
public sealed class NpcShoutsService
{
    private readonly PlayerConnectionRegistry _connRegistry;

    public NpcShoutsService(PlayerConnectionRegistry connRegistry)
    {
        _connRegistry = connRegistry;
    }

    /// <summary>
    /// Broadcasts an NPC-chat message (string id) to every online player currently sharing the NPC's
    /// world/instance scope. Fire-and-forget, mirroring Java's <c>ThreadPoolManager.schedule(..., 0)</c>
    /// dispatch so callers (NpcAi2.SendMsg, a synchronous script hook) don't need to be async.
    /// </summary>
    public void SendMsg(Npc? npc, int stringId)
    {
        if (npc is null || npc.IsAlreadyDead) return;

        var packet = SM_SYSTEM_MESSAGE.NpcShout(npc.ObjectId, stringId);
        var scope  = npc.Position;
        _ = Task.Run(async () =>
        {
            foreach (var conn in _connRegistry.GetAll())
                if (conn.ActivePlayer is { } player && player.Position.SameScope(scope))
                    try { await conn.SendAsync(packet); } catch { /* ignore disconnects */ }
        });
    }
}
