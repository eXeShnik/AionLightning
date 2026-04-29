using System.Text;
using AionLightning.Chat.Service;
using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Aion.ClientPackets;

public sealed class CM_PLAYER_AUTH : AionClientPacket
{
    private readonly AionClientConnection _conn;
    private readonly ChatService _chatService;

    private int _playerId;
    private byte[] _token = [];
    private byte[] _identifierBytes = [];
    private string _realName = string.Empty;

    public CM_PLAYER_AUTH(AionClientConnection conn, ChatService chatService)
    {
        _conn = conn;
        _chatService = chatService;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadB(29); // Aion preamble
        _playerId = r.ReadD();
        r.ReadD(); r.ReadD(); r.ReadD(); // zeroes
        int idCharCount = r.ReadH();
        byte[] rawId = r.ReadB(idCharCount * 2);
        int acctCharCount = r.ReadH();
        r.ReadB(acctCharCount * 2); // discard account name
        int tokenLen = r.ReadH();
        _token = r.ReadB(tokenLen);

        // identifier format: "realName@channelIdentifier"
        string full = Encoding.Unicode.GetString(rawId);
        int at = full.IndexOf('@');
        if (at >= 0)
        {
            _realName = full[..at];
            _identifierBytes = Encoding.Unicode.GetBytes(full[(at + 1)..]);
        }
        else
        {
            _realName = full;
            _identifierBytes = rawId;
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
        => await _chatService.RegisterPlayerConnectionAsync(_playerId, _token, _identifierBytes, _conn, _realName, ct);
}
