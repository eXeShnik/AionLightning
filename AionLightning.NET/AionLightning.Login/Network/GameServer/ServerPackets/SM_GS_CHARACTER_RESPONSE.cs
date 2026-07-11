using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.GameServer.ServerPackets;

/// <summary>
/// Asks the game server how many characters the given account has there.
/// GS answers with CM_GS_CHARACTER (opcode 8).
/// </summary>
public sealed class SM_GS_CHARACTER_RESPONSE : AionServerPacket
{
    private readonly int _accountId;

    public SM_GS_CHARACTER_RESPONSE(int accountId) : base(0x08)
    {
        _accountId = accountId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_accountId);
    }
}
