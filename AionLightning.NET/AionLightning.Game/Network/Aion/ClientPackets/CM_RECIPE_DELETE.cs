using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client deletes a crafting recipe. Stub — opcode 0x13B.</summary>
public sealed class CM_RECIPE_DELETE : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadD(); // recipeId
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
