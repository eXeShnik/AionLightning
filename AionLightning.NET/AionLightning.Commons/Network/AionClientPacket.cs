namespace AionLightning.Commons.Network;

/// <summary>
/// Base for all Aion client packets. Subclasses implement Read (deserialize body) and RunAsync (handle).
/// File name and class name keep Java UPPER_SNAKE conventions (CM_LOGIN, CM_PLAY, …).
/// </summary>
public abstract class AionClientPacket
{
    public byte Opcode { get; internal set; }

    /// <summary>Deserialize the packet body from the already-decrypted frame (opcode already consumed).</summary>
    public abstract void Read(ref PacketReader r);

    /// <summary>Execute packet logic after a successful Read.</summary>
    public abstract ValueTask RunAsync(CancellationToken ct);
}
