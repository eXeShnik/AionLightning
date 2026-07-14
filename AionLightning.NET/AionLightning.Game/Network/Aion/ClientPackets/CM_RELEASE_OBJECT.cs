using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Java clientpackets.CM_RELEASE_OBJECT — cancels the sending player's in-progress "use house object"
/// interaction (Java: aborts <c>TaskId.HOUSE_OBJECT_USE</c> when the target isn't a still-pending
/// UseableItemObject dialog, e.g. the postbox mailbox flow or a cook-pot's multi-second reward timer).
///
/// note: this is a distinct action from "pick up a placed object back to inventory" — that's
/// CM_HOUSE_EDIT's DESPAWN_OBJECT (see CM_HOUSE_EDIT.HandleDespawnObjectAsync). This port's
/// CM_USE_HOUSE_OBJECT applies its cooldown/use-count effect synchronously with no intervening
/// multi-second task (no mailbox/cook-pot reward flow is ported — see HouseObject's class doc), so there is
/// nothing pending left for this packet to cancel; it is a no-op here, kept only so the already-wired
/// opcode has a documented handler instead of silently discarding the packet.
/// Opcode 0x1A3.
/// </summary>
public sealed class CM_RELEASE_OBJECT : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IOptions<HousingOptions> _housingOptions;

    private int _targetObjectId;

    public CM_RELEASE_OBJECT(GsClientConnection conn, IOptions<HousingOptions> housingOptions)
    {
        _conn = conn;
        _housingOptions = housingOptions;
    }

    public override void Read(ref PacketReader r) => _targetObjectId = r.ReadD();

    public override ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable) return ValueTask.CompletedTask;
        if (_conn.ActivePlayer is null) return ValueTask.CompletedTask;

        // See class doc — nothing to cancel with this port's synchronous CM_USE_HOUSE_OBJECT.
        return ValueTask.CompletedTask;
    }
}
