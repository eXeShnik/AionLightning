using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts crafting animation to players in range. Opcode 0xB4.</summary>
public sealed class SM_CRAFT_ANIMATION : AionServerPacket
{
    private readonly int _senderObjectId;
    private readonly int _targetObjectId;
    private readonly int _skillId;
    private readonly int _action;

    /// <param name="action">1 = start, 2 = stop</param>
    public SM_CRAFT_ANIMATION(int senderObjectId, int targetObjectId, int skillId, int action) : base(0xB4)
    {
        _senderObjectId = senderObjectId;
        _targetObjectId = targetObjectId;
        _skillId        = skillId;
        _action         = action;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_senderObjectId);
        w.WriteD(_targetObjectId);
        w.WriteH((short)_skillId);
        w.WriteC((byte)_action);
    }
}
