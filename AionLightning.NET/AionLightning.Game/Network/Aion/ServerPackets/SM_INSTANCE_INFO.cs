using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends instance/group info to the client on enter-world.
/// For solo open-world play: type=0x02 (push, not answer), 1 player entry, no cooldowns.
/// Opcode 0x8D.
/// </summary>
public sealed class SM_INSTANCE_INFO : AionServerPacket
{
    private readonly Player _player;

    public SM_INSTANCE_INFO(Player player) : base(0x8D) => _player = player;

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x02); // not an answer (push)
        w.WriteC(0);    // cooldownId = 0
        w.WriteD(0);    // unk

        w.WriteH(1);               // 1 player entry (solo)
        w.WriteD(_player.ObjectId);
        w.WriteH(0);               // 0 instance cooldowns
        w.WriteS(_player.Name);
    }
}
