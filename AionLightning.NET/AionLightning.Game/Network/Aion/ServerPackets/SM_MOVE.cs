using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_MOVE : AionServerPacket
{
    private readonly Player _player;

    public SM_MOVE(Player player) : base(0x37) => _player = player;

    public override void Write(ref PacketWriter w)
    {
        var p = _player;
        w.WriteD(p.ObjectId);
        w.WriteF(p.Position.X);
        w.WriteF(p.Position.Y);
        w.WriteF(p.Position.Z);
        w.WriteC((byte)p.Position.Heading);
        w.WriteC(p.MovementMask);

        if ((p.MovementMask & MovementMask.StartMove) != 0)
        {
            if ((p.MovementMask & MovementMask.Mouse) == 0)
            {
                // Keyboard — send velocity vector
                w.WriteF(p.VectorX);
                w.WriteF(p.VectorY);
                w.WriteF(p.VectorZ);
            }
            else
            {
                // Mouse click — send target position
                w.WriteF(p.TargetX2);
                w.WriteF(p.TargetY2);
                w.WriteF(p.TargetZ2);
            }
        }
    }
}
