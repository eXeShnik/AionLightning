using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_CREATE_CHARACTER : AionServerPacket
{
    public const int RESPONSE_OK                    = 0;
    public const int FAILED_TO_CREATE               = 1;
    public const int RESPONSE_DB_ERROR              = 2;
    public const int RESPONSE_SERVER_LIMIT_EXCEEDED = 4;
    public const int RESPONSE_INVALID_NAME          = 5;
    public const int RESPONSE_FORBIDDEN_NAME        = 9;
    public const int RESPONSE_NAME_ALREADY_USED     = 10;
    public const int RESPONSE_NAME_RESERVED         = 11;
    public const int RESPONSE_CREATE_READY          = 20;

    private readonly int _responseCode;
    private readonly Player? _player;
    private readonly PlayerAppearance? _appearance;

    public SM_CREATE_CHARACTER(int responseCode, Player? player = null, PlayerAppearance? appearance = null)
        : base(0xC9)
    {
        _responseCode = responseCode;
        _player       = player;
        _appearance   = appearance;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_responseCode);

        if (_responseCode == RESPONSE_OK && _player is not null && _appearance is not null)
        {
            SM_CHARACTER_LIST.WritePlayerInfo(ref w, _player, _appearance);
            w.WriteZero(136);
        }
        else
        {
            w.WriteZero(616);
        }
    }
}
