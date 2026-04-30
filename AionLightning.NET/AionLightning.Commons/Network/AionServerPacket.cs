namespace AionLightning.Commons.Network;

/// <summary>
/// Base for all Aion server packets. Subclasses implement Write to serialize the body.
/// The length prefix and opcode byte are written by the connection's send path.
/// File name and class name keep Java UPPER_SNAKE conventions (SM_INIT, SM_LOGIN_FAIL, …).
/// </summary>
public abstract class AionServerPacket
{
    public ushort Opcode { get; }

    protected AionServerPacket(ushort opcode)
    {
        Opcode = opcode;
    }

    /// <summary>Write packet body (excluding length prefix; opcode is written by the send path).</summary>
    public abstract void Write(ref PacketWriter w);
}
