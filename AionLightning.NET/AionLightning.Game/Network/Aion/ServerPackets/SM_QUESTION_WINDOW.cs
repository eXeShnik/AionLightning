using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Opens a yes/no dialog on the Aion client. Opcode 0x34.
/// Common codes: 60000 = group invite, 50028 = duel request.
/// </summary>
public sealed class SM_QUESTION_WINDOW : AionServerPacket
{
    private readonly int      _code;
    private readonly int      _senderId;
    private readonly int      _range;
    private readonly string[] _vars;

    public SM_QUESTION_WINDOW(int code, int senderId, int range, params string[] vars)
        : base(0x34)
    {
        _code     = code;
        _senderId = senderId;
        _range    = range;
        _vars     = vars;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_code);
        foreach (var v in _vars) w.WriteS(v);
        w.WriteD(0);
        w.WriteD(0);
        w.WriteH(0);
        w.WriteC(_range > 0 ? (byte)1 : (byte)0);
        w.WriteD(_senderId);
        w.WriteD(_range);
    }
}
