using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Duel notification packet (opcode 0xB9). Type 0=started, 1=result.</summary>
public sealed class SM_DUEL : AionServerPacket
{
    private readonly byte   _type;
    private readonly int    _opponentId; // type 0
    private readonly byte   _resultId;   // type 1
    private readonly int    _msgId;      // type 1
    private readonly string _name;       // type 1

    private SM_DUEL(byte type, int opponentId = 0, byte resultId = 0, int msgId = 0, string name = "")
        : base(0xB9)
    {
        _type       = type;
        _opponentId = opponentId;
        _resultId   = resultId;
        _msgId      = msgId;
        _name       = name;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_type);
        if (_type == 0x00)
        {
            w.WriteD(_opponentId);
        }
        else
        {
            w.WriteC(_resultId);
            w.WriteD(_msgId);
            w.WriteS(_name);
        }
    }

    public static SM_DUEL Started(int opponentObjectId) => new(0x00, opponentId: opponentObjectId);

    // DUEL_WON: resultId=2, msgId=1300098
    public static SM_DUEL Won(string opponentName)  => new(0x01, resultId: 2, msgId: 1300098, name: opponentName);

    // DUEL_LOST: resultId=0, msgId=1300099
    public static SM_DUEL Lost(string opponentName) => new(0x01, resultId: 0, msgId: 1300099, name: opponentName);
}
