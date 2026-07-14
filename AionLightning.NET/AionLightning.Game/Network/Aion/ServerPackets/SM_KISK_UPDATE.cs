using AionLightning.Commons.Network;
using AionLightning.Game.Model.GameObjects;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Pushes a kisk's current member count / resurrect counter to bound players whenever it
/// changes (bind, resurrect used, despawn). Opcode 0xB0 (Java: "for 1.5.1.10 and 1.5.1.15").</summary>
public sealed class SM_KISK_UPDATE : AionServerPacket
{
    private readonly int _objectId;
    private readonly int _creatorId;
    private readonly int _useMask;
    private readonly int _currentMembers;
    private readonly int _maxMembers;
    private readonly int _remainingResurrects;
    private readonly int _maxResurrects;
    private readonly int _remainingLifetime;

    public SM_KISK_UPDATE(Kisk kisk) : base(0xB0)
    {
        _objectId            = kisk.Npc.ObjectId;
        _creatorId           = kisk.OwnerId;
        _useMask             = kisk.UseMask;
        _currentMembers      = kisk.MemberIds.Count;
        _maxMembers          = kisk.MaxMembers;
        _remainingResurrects = kisk.RemainingResurrects;
        _maxResurrects       = kisk.MaxResurrects;
        _remainingLifetime   = kisk.RemainingLifetime;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        w.WriteD(_creatorId);
        w.WriteD(_useMask);
        w.WriteD(_currentMembers);
        w.WriteD(_maxMembers);
        w.WriteD(_remainingResurrects);
        w.WriteD(_maxResurrects);
        w.WriteD(_remainingLifetime);
    }
}
