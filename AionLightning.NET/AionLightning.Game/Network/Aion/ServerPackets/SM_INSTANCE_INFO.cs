using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends instance cooldown info to the client. Opcode 0x8D.
/// action=0x02 = push on enter-world (not an answer); action=0x00 = answer to CM_INSTANCE_INFO query.
/// Portal cooldown tracking is not implemented; this always returns an empty cooldown list.
/// </summary>
public sealed class SM_INSTANCE_INFO : AionServerPacket
{
    private readonly Player _player;
    private readonly bool   _isAnswer;

    public SM_INSTANCE_INFO(Player player, bool isAnswer = false) : base(0x8D)
    {
        _player   = player;
        _isAnswer = isAnswer;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_isAnswer ? (byte)0x00 : (byte)0x02);
        w.WriteC(0);    // cooldownId = 0
        w.WriteD(0);    // unk

        w.WriteH(1);                 // 1 player entry (solo)
        w.WriteD(_player.ObjectId);
        w.WriteH(0);                 // 0 active cooldowns
        w.WriteS(_player.Name);
    }
}
