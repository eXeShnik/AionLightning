using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts a legion member's updated self-introduction text. Opcode 0x77.</summary>
public sealed class SM_LEGION_UPDATE_SELF_INTRO : AionServerPacket
{
    private readonly int    _playerObjId;
    private readonly string _selfIntro;

    public SM_LEGION_UPDATE_SELF_INTRO(int playerObjId, string selfIntro) : base(0x77)
    {
        _playerObjId = playerObjId;
        _selfIntro   = selfIntro;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjId);
        w.WriteS(_selfIntro);
    }
}
