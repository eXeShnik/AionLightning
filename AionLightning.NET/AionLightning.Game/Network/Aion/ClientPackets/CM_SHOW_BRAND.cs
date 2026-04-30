using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Group leader places a brand marker on a target object. Opcode 0x197.
/// Group leaders relay the brand to all members; solo players echo it to themselves.
/// </summary>
public sealed class CM_SHOW_BRAND : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int _brandId;
    private int _targetObjectId;

    public CM_SHOW_BRAND(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadD();              // action (unused)
        _brandId        = r.ReadD();
        _targetObjectId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var packet = new SM_SHOW_BRAND(_brandId, _targetObjectId);
        var group  = player.Group;

        if (group is not null && group.IsLeader(player.ObjectId))
        {
            foreach (var member in group.Members)
            {
                var mc = _connRegistry.Get(member.ObjectId);
                if (mc is not null)
                    try { await mc.SendAsync(packet, ct); } catch { }
            }
        }
        else if (group is null)
        {
            await _conn.SendAsync(packet, ct);
        }
    }
}
