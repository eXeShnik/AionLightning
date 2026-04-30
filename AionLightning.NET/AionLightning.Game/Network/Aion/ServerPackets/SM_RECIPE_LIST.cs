using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the player's known recipe list. Opcode 0xCF.</summary>
public sealed class SM_RECIPE_LIST : AionServerPacket
{
    private readonly IReadOnlyCollection<int> _recipeIds;

    public SM_RECIPE_LIST(IEnumerable<int> recipeIds) : base(0xCF)
        => _recipeIds = recipeIds as IReadOnlyCollection<int> ?? recipeIds.ToList();

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_recipeIds.Count);
        foreach (var id in _recipeIds)
        {
            w.WriteD(id);
            w.WriteC(0);
        }
    }
}
