using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Notifies client which map to load and where the player spawns.</summary>
public sealed class SM_PLAYER_SPAWN : AionServerPacket
{
    private readonly Player _player;

    public SM_PLAYER_SPAWN(Player player) : base(0x0F) => _player = player;

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_player.Position.WorldId);
        w.WriteD(_player.Position.WorldId); // world + channel
        w.WriteD(0);                         // unk
        w.WriteC(0);                         // isPersonal (0 = regular world)
        w.WriteF(_player.Position.X);
        w.WriteF(_player.Position.Y);
        w.WriteF(_player.Position.Z);
        w.WriteC((byte)_player.Position.Heading);
        w.WriteC(8);                          // 0 or 1 in newer protocols; 8 for 4.x
        w.WriteD(0);                          // new 2.5
        w.WriteD(0);
        w.WriteD(0);                          // auto-group buff id (unused)
        w.WriteC(0);                          // fast-track enabled
        w.WriteD(0);                          // 4.0 protocol changed
    }
}
