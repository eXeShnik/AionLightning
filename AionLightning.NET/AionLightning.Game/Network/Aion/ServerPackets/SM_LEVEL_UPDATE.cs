using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_LEVEL_UPDATE : AionServerPacket
{
    private readonly int _objectId;
    private readonly short _effect;
    private readonly short _level;

    public SM_LEVEL_UPDATE(int objectId, int effect, int level)
        : base(0x46)
    {
        _objectId = objectId;
        _effect   = (short)effect;
        _level    = (short)level;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        w.WriteH(_effect);
        w.WriteH(_level);
        w.WriteH(0);
    }
}
