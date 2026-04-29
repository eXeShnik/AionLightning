using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_ENTER_WORLD : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IPlayerDao _playerDao;
    private readonly IPlayerAppearanceDao _appearanceDao;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager _dataManager;

    private int _objectId;

    public CM_ENTER_WORLD(GsClientConnection conn, IPlayerDao playerDao,
        IPlayerAppearanceDao appearanceDao, GameWorld world, PlayerConnectionRegistry connRegistry,
        IDataManager dataManager)
    {
        _conn           = conn;
        _playerDao      = playerDao;
        _appearanceDao  = appearanceDao;
        _world          = world;
        _connRegistry   = connRegistry;
        _dataManager    = dataManager;
    }

    public override void Read(ref PacketReader r) => _objectId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = await _playerDao.FindByObjectIdAsync(_objectId, ct);
        if (player is null || player.AccountId != _conn.AccountId)
        {
            await _conn.DisposeAsync();
            return;
        }

        var appearance = await _appearanceDao.FindByPlayerIdAsync(_objectId, ct);
        if (appearance is null)
        {
            await _conn.DisposeAsync();
            return;
        }

        var tpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);

        if (player.MaxHp == 0)
        {
            player.MaxHp      = tpl?.MaxHp   ?? 1000;
            player.MaxMp      = tpl?.MaxMp   ?? 500;
            player.CurrentHp  = player.MaxHp;
            player.CurrentMp  = player.MaxMp;
        }

        player.Appearance   = appearance;
        _conn.ActivePlayer  = player;
        _conn.State         = GsClientConnection.AionState.IN_GAME;

        // Auto-learn skills for class + race up to current level
        for (int lvl = 1; lvl <= player.Level; lvl++)
        {
            foreach (var slt in _dataManager.SkillTree.GetTemplatesFor(player.PlayerClass, lvl, player.Race))
            {
                if (slt.AutoLearn)
                    player.Skills.AddSkill(slt.SkillId, slt.SkillLevel, slt.Stigma);
            }
        }

        _world.Add(player);
        _connRegistry.Register(player.ObjectId, _conn);

        await _playerDao.UpdateOnlineAsync(player.ObjectId, online: true, ct);

        // Enter-world sequence: signal character select state, send stats, then spawn
        await _conn.SendAsync(new SM_CHARACTER_SELECT(0), ct);
        await _conn.SendAsync(new SM_STATS_INFO(player, tpl, _dataManager.ExpTable), ct);
        await _conn.SendAsync(new SM_SKILL_LIST(player.Skills.AllSkills), ct);
        await _conn.SendAsync(new SM_PLAYER_SPAWN(player), ct);
        await _conn.SendAsync(new SM_GAME_TIME(), ct);
    }
}
