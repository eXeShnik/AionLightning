using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client sends a legion command (create/disband/leave/kick/etc.). Stub — opcode 0xCF.</summary>
public sealed class CM_LEGION : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        int exOpcode = r.ReadC();
        switch (exOpcode)
        {
            case 0x00: r.ReadD(); r.ReadS(); break; // create
            case 0x01: r.ReadD(); r.ReadS(); break; // invite
            case 0x02: r.ReadD(); r.ReadH(); break; // leave
            case 0x04: r.ReadD(); r.ReadS(); break; // kick
            case 0x05: r.ReadD(); r.ReadS(); break; // appoint brigade general
            case 0x06: r.ReadD(); r.ReadS(); break; // appoint centurion
            case 0x07: r.ReadD(); r.ReadS(); break; // demote
            case 0x08: r.ReadD(); r.ReadH(); break; // refresh
            case 0x09: r.ReadD(); r.ReadS(); break; // announcement
            case 0x0A: r.ReadD(); r.ReadS(); break; // self introduction
            case 0x0D: r.ReadH(); r.ReadH(); r.ReadH(); r.ReadH(); break; // permissions
            case 0x0E: r.ReadD(); r.ReadH(); break; // level up
            case 0x0F: r.ReadS(); r.ReadS(); break; // nickname
        }
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
