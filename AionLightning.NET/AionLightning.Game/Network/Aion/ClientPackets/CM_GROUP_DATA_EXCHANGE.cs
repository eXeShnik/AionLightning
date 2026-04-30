using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client sends group UI data (minimap markers, target indicators). Opcode 0x2ED.
/// action=1: echo back to sender only (self-display update).
/// action!=1: relay to all online group members (groupType 0=group, 1/2=alliance).
/// </summary>
public sealed class CM_GROUP_DATA_EXCHANGE : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private byte   _action;
    private byte   _groupType;
    private byte   _unk2;
    private byte[] _data = [];

    public CM_GROUP_DATA_EXCHANGE(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _action = (byte)r.ReadC();
        if (_action == 1)
        {
            int size = r.ReadD();
            if (size > 0 && size <= 5086)
                _data = r.ReadB(size);
        }
        else
        {
            _groupType = (byte)r.ReadC();
            _unk2      = (byte)r.ReadC();
            int size   = r.ReadD();
            if (size > 0 && size <= 5086)
                _data = r.ReadB(size);
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || _data.Length == 0) return;

        if (_action == 1)
        {
            // Echo back to sender (updates their own minimap display)
            await _conn.SendAsync(new SM_GROUP_DATA_EXCHANGE(_data), ct);
            return;
        }

        // Relay to group members (groupType 0 = party group)
        var group = player.Group;
        if (group is null) return;

        var relay = new SM_GROUP_DATA_EXCHANGE(_data, _action, _unk2);
        foreach (var member in group.Members)
        {
            if (member.ObjectId == player.ObjectId) continue;
            var mc = _connRegistry.Get(member.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(relay, ct); } catch { }
        }
    }
}
