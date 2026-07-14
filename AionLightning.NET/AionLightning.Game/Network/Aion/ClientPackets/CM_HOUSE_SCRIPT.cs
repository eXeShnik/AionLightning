using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Java clientpackets.CM_HOUSE_SCRIPT — the Housing Panel's decoration-script editor sends one slot's
/// content whenever the player saves it; a "deposit"/"delete" (totalSize &lt;= 0) clears the slot — Java's
/// own comment notes the client sends the same wire shape for both. Only ever touches the player's own
/// active (owned) house — Java resolves the same via <c>player.getActiveHouse()</c>; there is no separate
/// permission check for scripts in Java (unlike CM_HOUSE_EDIT, this isn't gated on decoration/renovation
/// mode either).
/// Opcode 0xFC.
/// </summary>
public sealed class CM_HOUSE_SCRIPT : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IHouseScriptsDao _houseScriptsDao;
    private readonly IOptions<HousingOptions> _housingOptions;

    private int _address;
    private int _scriptIndex;
    private int _totalSize;
    private int _compressedSize;
    private int _uncompressedSize;
    private byte[]? _stream;

    public CM_HOUSE_SCRIPT(GsClientConnection conn, IHouseScriptsDao houseScriptsDao, IOptions<HousingOptions> housingOptions)
    {
        _conn = conn;
        _houseScriptsDao = houseScriptsDao;
        _housingOptions = housingOptions;
    }

    public override void Read(ref PacketReader r)
    {
        _address = r.ReadD();
        _scriptIndex = r.ReadC();
        _totalSize = r.ReadH();
        if (_totalSize > 0)
        {
            _compressedSize = r.ReadD();
            if (_compressedSize < 8150)
            {
                _uncompressedSize = r.ReadD();
                _stream = r.ReadB(_compressedSize);
            }
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (!_housingOptions.Value.Enable) return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        if (_compressedSize > 8149)
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.HousingScriptOverflow(), ct);

        var house = player.ActiveHouse;
        if (house is null) return;

        var scripts = house.Registry.Scripts;

        // Deposit perhaps should send 0, while delete -1. But the client sends the same packet for both
        // (Java's own comment) — either way an empty (not null) payload clears the slot.
        string? content;
        bool hadPriorData;
        if (_totalSize <= 0)
            content = scripts.TryApply(_scriptIndex, [], 0, out hadPriorData);
        else
            content = scripts.TryApply(_scriptIndex, _stream, _uncompressedSize, out hadPriorData);

        if (content is not null)
        {
            if (hadPriorData)
                await _houseScriptsDao.UpdateScriptAsync(house.Id, _scriptIndex, content, ct);
            else
                await _houseScriptsDao.AddScriptAsync(house.Id, _scriptIndex, content, ct);
        }

        await _conn.SendAsync(new SM_HOUSE_SCRIPTS(_address, scripts, _scriptIndex, _scriptIndex), ct);
    }
}
