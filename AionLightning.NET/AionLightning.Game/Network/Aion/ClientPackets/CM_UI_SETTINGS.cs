using AionLightning.Commons.Network;
using AionLightning.Game.Dao;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client saves UI layout / shortcut bar data. Opcode 0xA8.</summary>
public sealed class CM_UI_SETTINGS : AionClientPacket
{
    private readonly GsClientConnection  _conn;
    private readonly IPlayerSettingsDao  _settingsDao;

    private byte   _settingsType;
    private byte[] _data = [];

    public CM_UI_SETTINGS(GsClientConnection conn, IPlayerSettingsDao settingsDao)
    {
        _conn        = conn;
        _settingsDao = settingsDao;
    }

    public override void Read(ref PacketReader r)
    {
        _settingsType = (byte)r.ReadC();
        r.ReadH();                      // unk
        r.ReadH();                      // size (ignored; use remaining)
        if (r.Remaining > 0)
            _data = r.ReadB((int)r.Remaining);
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || _data.Length == 0) return;

        // Only types 0–2 are meaningful UI blobs
        if (_settingsType > 2) return;

        switch (_settingsType)
        {
            case 0: player.UiSettings   = _data; break;
            case 1: player.Shortcuts    = _data; break;
            case 2: player.HouseBuddies = _data; break;
        }

        await _settingsDao.SaveSettingAsync(player.ObjectId, _settingsType, _data, ct);
    }
}
