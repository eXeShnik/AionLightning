using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_CHARACTER_LIST : AionClientPacket
{
    private readonly GsClientConnection  _conn;
    private readonly IPlayerDao          _playerDao;
    private readonly IPlayerAppearanceDao _appearanceDao;
    private readonly IItemDao            _itemDao;
    private readonly IDataManager        _dataManager;
    private readonly IMailDao            _mailDao;

    private int _playOk2;

    public CM_CHARACTER_LIST(GsClientConnection conn, IPlayerDao playerDao,
        IPlayerAppearanceDao appearanceDao, IItemDao itemDao, IDataManager dataManager,
        IMailDao mailDao)
    {
        _conn          = conn;
        _playerDao     = playerDao;
        _appearanceDao = appearanceDao;
        _itemDao       = itemDao;
        _dataManager   = dataManager;
        _mailDao       = mailDao;
    }

    public override void Read(ref PacketReader r) => _playOk2 = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var players = await _playerDao.FindByAccountIdAsync(_conn.AccountId, ct);
        var characters = new List<(Player, PlayerAppearance, IReadOnlyList<Item>, bool)>(players.Count);

        foreach (var p in players)
        {
            var appearance = await _appearanceDao.FindByPlayerIdAsync(p.ObjectId, ct)
                             ?? new PlayerAppearance();

            var allItems = await _itemDao.FindByPlayerIdAsync(p.ObjectId, ct);
            var equipment = allItems
                .Where(i => i.IsEquipped && i.Slot > 0 && i.Slot <= 4096)
                .Where(i =>
                {
                    var tpl = _dataManager.Items.GetTemplate(i.ItemId);
                    return tpl is not null && (tpl.IsWeapon || tpl.IsArmor);
                })
                .ToList();

            bool hasUnread = await _mailDao.HasUnreadAsync(p.ObjectId, ct);
            characters.Add((p, appearance, equipment, hasUnread));
        }

        await _conn.SendAsync(new SM_CHARACTER_LIST(_playOk2, characters), ct);
    }
}
