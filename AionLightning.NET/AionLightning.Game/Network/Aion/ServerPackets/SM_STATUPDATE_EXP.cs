using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_STATUPDATE_EXP : AionServerPacket
{
    private readonly long _currentExp;
    private readonly long _recoverableExp;
    private readonly long _maxExp;

    public SM_STATUPDATE_EXP(long currentExp, long recoverableExp, long maxExp)
        : base(0x08)
    {
        _currentExp     = currentExp;
        _recoverableExp = recoverableExp;
        _maxExp         = maxExp;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteQ(_currentExp);
        w.WriteQ(_recoverableExp);
        w.WriteQ(_maxExp);
        w.WriteQ(0); // boost exp current (repose energy)
        w.WriteQ(0); // boost exp max
        w.WriteQ(0); // event exp
    }
}
