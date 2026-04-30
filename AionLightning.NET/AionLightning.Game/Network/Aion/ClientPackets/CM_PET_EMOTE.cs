using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client triggers a pet emote. Stub — opcode 0xF7.</summary>
public sealed class CM_PET_EMOTE : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        int emoteId = r.ReadC();
        if (emoteId == 0) // MOVE_STOP
        {
            r.ReadF(); r.ReadF(); r.ReadF(); r.ReadC(); // x, y, z, heading
        }
        else if (emoteId == 12) // MOVETO
        {
            r.ReadF(); r.ReadF(); r.ReadF(); r.ReadC(); // src x, y, z, h
            r.ReadF(); r.ReadF(); r.ReadF();             // dst x, y, z
        }
        else
        {
            r.ReadC(); r.ReadC(); // emotionId, unk2
        }
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
