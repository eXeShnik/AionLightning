using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Model.GameObjects;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the player's active bind point (obelisk or kisk). Opcode 0xEB.</summary>
public sealed class SM_BIND_POINT_INFO : AionServerPacket
{
    private readonly int   _worldId;
    private readonly float _x, _y, _z;
    private readonly Kisk? _kisk;

    /// <param name="kisk">The player's currently-bound kisk, or null for a plain obelisk bind
    /// (Java SM_BIND_POINT_INFO reads this from Player.getKisk() internally).</param>
    public SM_BIND_POINT_INFO(Position pos, Kisk? kisk = null) : base(0xEB)
    {
        _worldId = pos.WorldId;
        _x       = pos.X;
        _y       = pos.Y;
        _z       = pos.Z;
        _kisk    = kisk;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_kisk is null ? (byte)0x00 : (byte)0x04); // 0x04 = bound to a kisk, 0x00 = obelisk only
        w.WriteC(0x01); // unk
        w.WriteD(_worldId);
        w.WriteF(_x);
        w.WriteF(_y);
        w.WriteF(_z);
        w.WriteD(_kisk is not null && _kisk.IsActive ? _kisk.Npc.ObjectId : 0);
    }
}
