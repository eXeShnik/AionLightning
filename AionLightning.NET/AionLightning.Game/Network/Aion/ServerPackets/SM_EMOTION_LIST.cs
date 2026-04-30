using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the player's unlocked emotion/emote list. Opcode 0x4F.</summary>
public sealed class SM_EMOTION_LIST : AionServerPacket
{
    private readonly byte _action;

    public SM_EMOTION_LIST(byte action = 0) : base(0x4F) => _action = action;

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        w.WriteH(0); // count = 0 (no custom emotions)
    }
}
