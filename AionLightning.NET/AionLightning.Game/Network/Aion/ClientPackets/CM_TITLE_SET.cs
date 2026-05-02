using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client equips or unequips an active title. Opcode 0x129.</summary>
public sealed class CM_TITLE_SET : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly IPlayerDao               _playerDao;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager             _dataManager;

    private short _titleId;

    public CM_TITLE_SET(GsClientConnection conn, IPlayerDao playerDao,
        PlayerConnectionRegistry connRegistry, IDataManager dataManager)
    {
        _conn         = conn;
        _playerDao    = playerDao;
        _connRegistry = connRegistry;
        _dataManager  = dataManager;
    }

    public override void Read(ref PacketReader r) => _titleId = (short)r.ReadH();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // 0xFFFF (as signed short = -1) means unequip
        int titleId = _titleId == -1 ? -1 : _titleId;
        player.TitleId = titleId;

        // Apply title stat modifiers (MAXHP/MAXMP <add> bonuses from player_titles.xml)
        var titleTpl = titleId > 0 ? _dataManager.Titles.GetTemplate(titleId) : null;
        player.TitleBonusMaxHp = titleTpl?.GetAddStat("MAXHP") ?? 0;
        player.TitleBonusMaxMp = titleTpl?.GetAddStat("MAXMP") ?? 0;

        // Recompute MaxHp/MaxMp to include the new title bonus
        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        float ssMult = player.SoulSicknessMultiplier;
        player.MaxHp = (int)(((statTpl?.MaxHp ?? 1000) + player.BonusMaxHp + player.PassiveBonusMaxHp + player.TitleBonusMaxHp) * ssMult);
        player.MaxMp = (int)(((statTpl?.MaxMp ?? 500)  + player.BonusMaxMp + player.PassiveBonusMaxMp + player.TitleBonusMaxMp) * ssMult);
        player.CurrentHp = Math.Min(player.CurrentHp, player.MaxHp);
        player.CurrentMp = Math.Min(player.CurrentMp, player.MaxMp);

        await _playerDao.UpdateTitleAsync(player.ObjectId, titleId, ct);
        await _conn.SendAsync(SM_TITLE_INFO.ActiveTitle(titleId), ct);
        await _conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);

        // Broadcast the title change to zone peers
        var broadcast = SM_TITLE_INFO.BroadcastTitle(player.ObjectId, titleId);
        int worldId   = player.Position.WorldId;
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(broadcast, ct); } catch { }
    }
}
