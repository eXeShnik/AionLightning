using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client issues a pet command. Stub — opcode 0xF4.</summary>
public sealed class CM_PET : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        int actionId = r.ReadH();
        switch (actionId)
        {
            case 1: // ADOPT
                r.ReadD(); r.ReadD(); r.ReadC(); r.ReadD(); r.ReadD(); r.ReadD(); r.ReadD(); r.ReadS();
                break;
            case 2: // SURRENDER
            case 3: // SPAWN
            case 4: // DISMISS
                r.ReadD();
                break;
            case 9: // FOOD
                int actionType = r.ReadD();
                if (actionType == 3)
                    r.ReadD(); // activateLoot
                else
                {
                    r.ReadD(); r.ReadD(); r.ReadD(); // dopingAction/objectId + 2 more
                }
                break;
            case 10: // RENAME
                r.ReadD(); r.ReadS();
                break;
            case 12: // MOOD
                r.ReadD(); r.ReadD();
                break;
        }
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
