using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends motion (combat animation style) data. Opcode 0x94.</summary>
public sealed class SM_MOTION : AionServerPacket
{
    private readonly byte _action;
    private readonly int _playerObjectId;

    /// <summary>action=1: sends own motion list to self (enter-world).</summary>
    public static SM_MOTION OwnList() => new(1, 0);

    /// <summary>action=7: broadcasts active motion slots for another player.</summary>
    public static SM_MOTION Broadcast(int playerObjectId) => new(7, playerObjectId);

    private SM_MOTION(byte action, int playerObjectId) : base(0x94)
    {
        _action         = action;
        _playerObjectId = playerObjectId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        switch (_action)
        {
            case 1:
                w.WriteH(0); // no motions
                break;
            case 7:
                w.WriteD(_playerObjectId);
                for (int i = 0; i < 5; i++)
                    w.WriteH(0); // no active motions in each slot
                break;
        }
    }
}
