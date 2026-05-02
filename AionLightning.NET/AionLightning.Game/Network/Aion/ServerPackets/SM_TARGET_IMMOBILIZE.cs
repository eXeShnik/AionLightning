using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Snaps a creature's position for all observers when a movement-blocking CC is applied (root, stun, paralyze, etc.).
/// Opcode 0xCC. Java: SM_TARGET_IMMOBILIZE — sent by RootEffect, StunEffect, StunAlwaysEffect, FearEffect.
/// </summary>
public sealed class SM_TARGET_IMMOBILIZE : AionServerPacket
{
    private readonly int   _objectId;
    private readonly float _x, _y, _z;
    private readonly int   _heading;

    public SM_TARGET_IMMOBILIZE(Creature creature) : base(0xCC)
    {
        _objectId = creature.ObjectId;
        _x        = creature.Position.X;
        _y        = creature.Position.Y;
        _z        = creature.Position.Z;
        _heading  = creature.Position.Heading;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        w.WriteF(_x);
        w.WriteF(_y);
        w.WriteF(_z);
        w.WriteC((byte)_heading);
    }
}
