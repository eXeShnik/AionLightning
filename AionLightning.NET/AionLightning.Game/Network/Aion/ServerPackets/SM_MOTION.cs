using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends motion (combat animation style) data. Opcode 0x94.</summary>
public sealed class SM_MOTION : AionServerPacket
{
    private readonly byte  _action;
    private readonly int   _playerObjectId;
    private readonly short _motionId;
    private readonly byte  _slot;
    private readonly Dictionary<byte, short>? _motions;

    /// <summary>action=1: sends own motion list to self (enter-world).</summary>
    public static SM_MOTION OwnList(Dictionary<byte, short> motions) => new(1, 0, 0, 0, motions);

    /// <summary>action=5: activates a single motion slot (broadcast to zone).</summary>
    public static SM_MOTION SetSlot(short motionId, byte slot) => new(5, 0, motionId, slot, null);

    /// <summary>action=7: broadcasts all active motion slots for another player.</summary>
    public static SM_MOTION Broadcast(int playerObjectId, Dictionary<byte, short> motions)
        => new(7, playerObjectId, 0, 0, motions);

    private SM_MOTION(byte action, int playerObjectId, short motionId, byte slot,
        Dictionary<byte, short>? motions) : base(0x94)
    {
        _action         = action;
        _playerObjectId = playerObjectId;
        _motionId       = motionId;
        _slot           = slot;
        _motions        = motions;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        switch (_action)
        {
            case 1:
                var list = _motions ?? new Dictionary<byte, short>();
                w.WriteH((short)list.Count);
                foreach (var (slot, id) in list)
                {
                    w.WriteH(id);
                    w.WriteD(0); // remaining time (0 = permanent)
                    w.WriteC(slot);
                }
                break;
            case 5:
                w.WriteH(_motionId);
                w.WriteC(_slot);
                break;
            case 7:
                w.WriteD(_playerObjectId);
                var active = _motions ?? new Dictionary<byte, short>();
                for (byte i = 1; i <= 5; i++)
                    w.WriteH(active.TryGetValue(i, out var mid) ? mid : (short)0);
                break;
        }
    }
}
