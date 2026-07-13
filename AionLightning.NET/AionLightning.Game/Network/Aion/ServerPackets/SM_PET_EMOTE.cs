using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Broadcast of a toy-pet emote/movement (Java <c>SM_PET_EMOTE</c>, opcode 0xBB). Pet movement is
/// client-driven — the owning client sends CM_PET_EMOTE and the server relays this to nearby players
/// so their clients render the same motion/emote.
/// </summary>
public sealed class SM_PET_EMOTE : AionServerPacket
{
    private const int MoveStop = 0;
    private const int MoveTo   = 12;

    private readonly int _petObjectId;
    private readonly int _emoteId;
    private float _x, _y, _z, _x2, _y2, _z2;
    private byte _heading;
    private int _emotionId, _param1;

    private SM_PET_EMOTE(int petObjectId, int emoteId) : base(0xBB)
    {
        _petObjectId = petObjectId;
        _emoteId     = emoteId;
    }

    public static SM_PET_EMOTE Stop(int petObjectId, float x, float y, float z, byte heading)
        => new(petObjectId, MoveStop) { _x = x, _y = y, _z = z, _heading = heading };

    public static SM_PET_EMOTE Move(int petObjectId, float x, float y, float z, byte heading, float x2, float y2, float z2)
        => new(petObjectId, MoveTo) { _x = x, _y = y, _z = z, _heading = heading, _x2 = x2, _y2 = y2, _z2 = z2 };

    public static SM_PET_EMOTE Emotion(int petObjectId, int emoteId, int emotionId, int param1)
        => new(petObjectId, emoteId) { _emotionId = emotionId, _param1 = param1 };

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_petObjectId);
        w.WriteC((byte)_emoteId);
        switch (_emoteId)
        {
            case MoveStop:
                w.WriteF(_x); w.WriteF(_y); w.WriteF(_z); w.WriteC(_heading);
                break;
            case MoveTo:
                w.WriteF(_x); w.WriteF(_y); w.WriteF(_z); w.WriteC(_heading);
                w.WriteF(_x2); w.WriteF(_y2); w.WriteF(_z2);
                break;
            default:
                w.WriteC((byte)_emotionId); w.WriteC((byte)_param1);
                break;
        }
    }
}
