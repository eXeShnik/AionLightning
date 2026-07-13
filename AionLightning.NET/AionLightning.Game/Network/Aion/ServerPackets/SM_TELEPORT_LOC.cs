using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Teleports the client to a new position (possibly a different map). Opcode 0x14.
/// portAnimation: 0 = jump animation, 1 = no animation.
/// </summary>
public sealed class SM_TELEPORT_LOC : AionServerPacket
{
    private readonly byte _portAnimation;
    private readonly int _mapId;
    private readonly int _instanceId;
    private readonly float _x, _y, _z;
    private readonly byte _heading;

    public SM_TELEPORT_LOC(int mapId, float x, float y, float z,
        byte heading = 0, byte portAnimation = 0, int instanceId = 0)
        : base(0x14)
    {
        _portAnimation = portAnimation;
        _mapId         = mapId;
        // Open-world callers pass instanceId 0 → the client expects the map id in the channel field
        // (open world is "channel 1" == the map itself). Instance callers pass the allocated channel.
        _instanceId    = instanceId == 0 ? mapId : instanceId;
        _x             = x;
        _y             = y;
        _z             = z;
        _heading       = heading;
    }

    public SM_TELEPORT_LOC(Position pos, byte portAnimation = 0)
        : this(pos.WorldId, pos.X, pos.Y, pos.Z, (byte)pos.Heading, portAnimation, pos.InstanceId) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_portAnimation);
        w.WriteD(_mapId);
        w.WriteD(_instanceId); // channel/instance id (mapId for open-world, allocated channel for instances)
        w.WriteF(_x);
        w.WriteF(_y);
        w.WriteF(_z);
        w.WriteC(_heading);
    }
}
