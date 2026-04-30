using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client edits character appearance (barbershop). Stub — opcode 0xA5.</summary>
public sealed class CM_CHARACTER_EDIT : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // objectId (character to edit)
        r.ReadB(52); // appearance data blob
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
