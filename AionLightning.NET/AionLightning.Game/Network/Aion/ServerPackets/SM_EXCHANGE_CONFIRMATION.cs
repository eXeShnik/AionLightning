using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Exchange state notification. Opcode 0x4E.
/// action: 1=partner locked, 3=trade success, 4=cancelled, 8=window opened.
/// </summary>
public sealed class SM_EXCHANGE_CONFIRMATION : AionServerPacket
{
    public enum Action : byte
    {
        Opened        = 8,
        PartnerLocked = 1,
        Success       = 3,
        Cancelled     = 4,
    }

    private readonly Action _action;

    public SM_EXCHANGE_CONFIRMATION(Action action) : base(0x4E)
        => _action = action;

    public override void Write(ref PacketWriter w) => w.WriteC((byte)_action);
}
