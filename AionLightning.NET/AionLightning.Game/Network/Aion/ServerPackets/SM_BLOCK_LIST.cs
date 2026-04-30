using AionLightning.Commons.Network;
using AionLightning.Game.Model.Social;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the player's block list. Opcode 0xE0.</summary>
public sealed class SM_BLOCK_LIST : AionServerPacket
{
    private readonly IReadOnlyList<BlockEntry> _blocks;

    public SM_BLOCK_LIST(IReadOnlyList<BlockEntry> blocks) : base(0xE0)
        => _blocks = blocks;

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_blocks.Count);
        foreach (var b in _blocks)
        {
            w.WriteD(b.PlayerId);
            w.WriteS(b.Name);
            w.WriteS(b.Reason);
        }
        w.WriteC(0);
    }
}
