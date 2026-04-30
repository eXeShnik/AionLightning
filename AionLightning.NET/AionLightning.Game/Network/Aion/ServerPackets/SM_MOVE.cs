using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_MOVE : AionServerPacket
{
    private readonly Player?  _player;
    private readonly int      _objectId;
    private readonly float    _x, _y, _z;
    private readonly byte     _heading;
    private readonly byte     _mask;
    private readonly float    _tx, _ty, _tz;

    // Player movement (existing path)
    public SM_MOVE(Player player) : base(0x37)
    {
        _player = player;
    }

    // NPC movement — move toward (tx,ty,tz)
    public static SM_MOVE StartNpcMove(int objectId, float x, float y, float z, byte heading,
        float tx, float ty, float tz)
        => new(objectId, x, y, z, heading, MovementMask.StartMove, tx, ty, tz);

    // NPC stop — announce final resting position
    public static SM_MOVE StopNpcMove(int objectId, float x, float y, float z, byte heading)
        => new(objectId, x, y, z, heading, 0, 0, 0, 0);

    private SM_MOVE(int objectId, float x, float y, float z, byte heading, byte mask,
        float tx, float ty, float tz) : base(0x37)
    {
        _objectId = objectId;
        _x        = x;
        _y        = y;
        _z        = z;
        _heading  = heading;
        _mask     = mask;
        _tx       = tx;
        _ty       = ty;
        _tz       = tz;
    }

    public override void Write(ref PacketWriter w)
    {
        if (_player is not null)
        {
            WritePlayer(ref w);
            return;
        }

        // NPC path
        w.WriteD(_objectId);
        w.WriteF(_x);
        w.WriteF(_y);
        w.WriteF(_z);
        w.WriteC(_heading);
        w.WriteC(_mask);
        if ((_mask & MovementMask.StartMove) != 0)
        {
            w.WriteF(_tx);
            w.WriteF(_ty);
            w.WriteF(_tz);
        }
    }

    private void WritePlayer(ref PacketWriter w)
    {
        var p = _player!;
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
                w.WriteF(p.VectorX);
                w.WriteF(p.VectorY);
                w.WriteF(p.VectorZ);
            }
            else
            {
                w.WriteF(p.TargetX2);
                w.WriteF(p.TargetY2);
                w.WriteF(p.TargetZ2);
            }
        }
    }
}
