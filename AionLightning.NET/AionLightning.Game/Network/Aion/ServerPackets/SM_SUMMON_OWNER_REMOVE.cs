using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Tells the master's client to drop its reference to a released summon. Opcode 0x9A.</summary>
public sealed class SM_SUMMON_OWNER_REMOVE : AionServerPacket
{
    private readonly int _summonObjId;

    public SM_SUMMON_OWNER_REMOVE(int summonObjId) : base(0x9A) => _summonObjId = summonObjId;

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_summonObjId);
    }
}
