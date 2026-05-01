using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Confirms the player's buddy-list status change (online/invisible/busy). Opcode 0xE3.</summary>
public sealed class SM_FRIEND_STATUS : AionServerPacket
{
    private readonly byte _status;

    public SM_FRIEND_STATUS(byte status) : base(0xE3)
    {
        _status = status;
    }

    public override void Write(ref PacketWriter w) => w.WriteC(_status);
}
