using AionLightning.Commons.Network;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client fuses two two-hand weapons at an NPC. Opcode 0x16C.
/// Thin controller — validation, kinah charge, and item mutation all live in
/// <see cref="ArmsfusionService.FuseAsync"/> so the class-change/quest-script equivalents (if any are
/// ever added) can't drift from this entry point.
/// </summary>
public sealed class CM_FUSION_WEAPONS : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly ArmsfusionService  _armsfusion;

    private int _unk;
    private int _firstItemObjId;
    private int _secondItemObjId;

    public CM_FUSION_WEAPONS(GsClientConnection conn, ArmsfusionService armsfusion)
    {
        _conn       = conn;
        _armsfusion = armsfusion;
    }

    public override void Read(ref PacketReader r)
    {
        _unk             = r.ReadD();
        _firstItemObjId  = r.ReadD();
        _secondItemObjId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        await _armsfusion.FuseAsync(player, _firstItemObjId, _secondItemObjId, _conn, ct);
    }
}
