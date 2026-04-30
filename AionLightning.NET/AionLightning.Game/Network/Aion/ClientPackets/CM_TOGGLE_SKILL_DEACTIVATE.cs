using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client deactivates a toggle skill (stance off). Sends SM_PLAYER_STANCE(0) to self. Opcode 0xE0.</summary>
public sealed class CM_TOGGLE_SKILL_DEACTIVATE : AionClientPacket
{
    private readonly GsClientConnection _conn;

    private short _skillId;

    public CM_TOGGLE_SKILL_DEACTIVATE(GsClientConnection conn) => _conn = conn;

    public override void Read(ref PacketReader r) => _skillId = r.ReadH();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        await _conn.SendAsync(new SM_PLAYER_STANCE(player.ObjectId, 0), ct);
    }
}
