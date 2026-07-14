using AionLightning.Commons.Network;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client breaks apart a fused weapon, removing the secondary fusion. Opcode 0x16D.
/// Thin controller — see <see cref="ArmsfusionService.BreakFusionAsync"/>.
/// </summary>
public sealed class CM_BREAK_WEAPONS : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly ArmsfusionService  _armsfusion;

    private int _unk;
    private int _weaponObjId;

    public CM_BREAK_WEAPONS(GsClientConnection conn, ArmsfusionService armsfusion)
    {
        _conn       = conn;
        _armsfusion = armsfusion;
    }

    public override void Read(ref PacketReader r)
    {
        _unk         = r.ReadD();
        _weaponObjId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        await _armsfusion.BreakFusionAsync(player, _weaponObjId, _conn, ct);
    }
}
