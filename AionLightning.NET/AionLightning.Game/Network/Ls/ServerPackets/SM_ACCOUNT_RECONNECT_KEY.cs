using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Ls.ServerPackets;

/// <summary>GS→LS: request a reconnect key for the given account. Opcode 0x02.</summary>
public sealed class SM_ACCOUNT_RECONNECT_KEY : AionServerPacket
{
    private readonly int _accountId;

    public SM_ACCOUNT_RECONNECT_KEY(int accountId) : base(0x02)
        => _accountId = accountId;

    public override void Write(ref PacketWriter w) => w.WriteD(_accountId);
}
