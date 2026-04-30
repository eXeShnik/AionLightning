using AionLightning.Commons.Events;
using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion.ClientPackets;
using AionLightning.Game.Network.Cs;
using AionLightning.Game.Services;
using AionLightning.Game.Model.Mail;
using GameWorld = AionLightning.Game.World.World;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion;

public sealed class GsPacketHandlerFactory
{
    private readonly ILogger<GsPacketHandlerFactory> _log;
    private readonly ILoggerFactory _loggerFactory;
    private readonly GameServerInfoOptions _gsInfo;
    private readonly NetworkOptions _network;
    private readonly CsConnectionOptions _csOpts;
    private readonly IPlayerDao _playerDao;
    private readonly IPlayerAppearanceDao _appearanceDao;
    private readonly IItemDao _itemDao;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IEventBus _eventBus;
    private readonly CsConnectionHolder _csHolder;
    private readonly IDataManager _dataManager;
    private readonly ExperienceService _expService;
    private readonly SpawnService _spawnService;
    private readonly LootService    _lootService;
    private readonly ISocialDao     _socialDao;
    private readonly IMailDao       _mailDao;
    private readonly ExchangeService _exchangeService;
    private readonly GroupService    _groupService;
    private readonly IQuestDao       _questDao;
    private readonly IMacroDao       _macroDao;
    private readonly QuestService    _questService;
    private readonly DuelService     _duelService;
    private readonly LegionService      _legionService;
    private readonly ILegionDao         _legionDao;
    private readonly IPlayerSettingsDao _settingsDao;

    public GsPacketHandlerFactory(
        ILogger<GsPacketHandlerFactory> log,
        ILoggerFactory loggerFactory,
        IOptions<GameServerInfoOptions> gsInfo,
        IOptions<NetworkOptions> network,
        IOptions<CsConnectionOptions> csOpts,
        IPlayerDao playerDao,
        IPlayerAppearanceDao appearanceDao,
        IItemDao itemDao,
        GameWorld world,
        PlayerConnectionRegistry connRegistry,
        IEventBus eventBus,
        CsConnectionHolder csHolder,
        IDataManager dataManager,
        ExperienceService expService,
        SpawnService spawnService,
        LootService lootService,
        ISocialDao socialDao,
        IMailDao mailDao,
        ExchangeService exchangeService,
        GroupService groupService,
        IQuestDao questDao,
        IMacroDao macroDao,
        QuestService questService,
        DuelService duelService,
        LegionService legionService,
        ILegionDao legionDao,
        IPlayerSettingsDao settingsDao)
    {
        _log           = log;
        _loggerFactory = loggerFactory;
        _gsInfo        = gsInfo.Value;
        _network       = network.Value;
        _csOpts        = csOpts.Value;
        _playerDao     = playerDao;
        _appearanceDao = appearanceDao;
        _itemDao       = itemDao;
        _world         = world;
        _connRegistry  = connRegistry;
        _eventBus      = eventBus;
        _csHolder      = csHolder;
        _dataManager   = dataManager;
        _expService    = expService;
        _spawnService  = spawnService;
        _lootService     = lootService;
        _socialDao       = socialDao;
        _mailDao         = mailDao;
        _exchangeService = exchangeService;
        _groupService    = groupService;
        _questDao        = questDao;
        _macroDao        = macroDao;
        _questService    = questService;
        _duelService     = duelService;
        _legionService   = legionService;
        _legionDao       = legionDao;
        _settingsDao     = settingsDao;
    }

    public AionClientPacket? Resolve(ushort opcode, GsClientConnection.AionState state, GsClientConnection conn)
    {
        return state switch
        {
            GsClientConnection.AionState.CONNECTED => opcode switch
            {
                0xC2  => new CM_VERSION_CHECK(conn, _gsInfo, _network, _csOpts),
                0xD0  => new CM_TIME_CHECK(conn),
                0x19F => new CM_MAC_ADDRESS(),
                _     => Unknown(state, opcode),
            },
            GsClientConnection.AionState.AUTHED => opcode switch
            {
                0xA5  => new CM_CHARACTER_EDIT(),
                0xA6  => new CM_MAY_QUIT(),
                0xAA  => new CM_ENTER_WORLD(conn, _playerDao, _appearanceDao, _itemDao, _questDao, _world, _connRegistry, _dataManager, _mailDao, _macroDao, _socialDao, _legionDao, _legionService, _settingsDao),
                0xC1  => new CM_QUIT(conn),
                0xCE  => new CM_PING(conn),
                0xD0  => new CM_TIME_CHECK(conn),
                0x173 => new CM_CHECK_NICKNAME(conn, _playerDao),
                0x174 => new CM_CHARACTER_LIST(conn, _playerDao, _appearanceDao, _itemDao, _dataManager, _mailDao),
                0x175 => new CM_CREATE_CHARACTER(conn, _playerDao, _appearanceDao, _dataManager),
                0x17A => new CM_DELETE_CHARACTER(conn, _playerDao),
                0x17B => new CM_RESTORE_CHARACTER(conn, _playerDao),
                0x190 => new CM_CHARACTER_PASSKEY(),
                0x195 => new CM_RECONNECT_AUTH(),
                0x198 => new CM_MAY_LOGIN_INTO_GAME(conn),
                0x19F => new CM_MAC_ADDRESS(),
                _     => Unknown(state, opcode),
            },
            GsClientConnection.AionState.IN_GAME => opcode switch
            {
                0xA6  => new CM_MAY_QUIT(),
                0xA7  => new CM_REVIVE(conn, _connRegistry, _dataManager),
                0xA8  => new CM_UI_SETTINGS(conn, _settingsDao),
                0xA9  => new CM_OBJECT_SEARCH(),
                0xAB  => new CM_LEVEL_READY(conn, _world, _connRegistry, _eventBus),
                0xAC  => new CM_CAPTCHA(),
                0xAD  => new CM_TELEPORT_DONE(),
                0xAE  => new CM_CUSTOM_SETTINGS(conn, _connRegistry, _playerDao),
                0xC1  => new CM_QUIT(conn),
                0xC4  => new CM_EQUIP_ITEM(conn, _itemDao, _connRegistry),
                0xC5  => new CM_CHAT_PLAYER_INFO(conn, _connRegistry),
                0xC7  => new CM_USE_ITEM(conn, _itemDao, _dataManager),
                0xC8  => new CM_GM_COMMAND_SEND(conn, _world, _connRegistry, _itemDao, _dataManager, _playerDao),
                0xC9  => new CM_EMOTION(conn, _connRegistry),
                0xCA  => new CM_PLAYER_LISTENER(),
                0xCC  => new CM_INSTANCE_LEAVE(),
                0xCD  => new CM_LEGION_SEND_EMBLEM(),
                0xCE  => new CM_PING(conn),
                0xCF  => new CM_LEGION(conn, _legionService, _legionDao, _connRegistry),
                0xD0  => new CM_TIME_CHECK(conn),
                0xD1  => new CM_GATHER(),
                0xD2  => new CM_LEGION_SEND_EMBLEM_INFO(),
                0xE0  => new CM_TOGGLE_SKILL_DEACTIVATE(conn),
                0xE1  => new CM_REMOVE_ALTERED_STATE(),
                0xE2  => new CM_ATTACK(conn, _world, _connRegistry, _expService, _spawnService, _lootService, _questService, _duelService),
                0xE3  => new CM_CASTSPELL(conn, _world, _connRegistry, _dataManager, _expService, _spawnService, _lootService, _questService, _duelService),
                0xF0  => new CM_QUESTION_RESPONSE(),
                0xF1  => new CM_BUY_ITEM(conn, _itemDao, _dataManager, _world),
                0xF2  => new CM_MOVE(conn, _world, _connRegistry),
                0xF3  => new CM_MOVE_IN_AIR(conn),
                0xF4  => new CM_PET(),
                0xF5  => new CM_OPEN_STATICDOOR(conn, _connRegistry),
                0xF6  => new CM_PETITION(),
                0xF7  => new CM_PET_EMOTE(),
                0xF9  => new CM_CHAT_MESSAGE_PUBLIC(conn, _connRegistry),
                0xFC  => new CM_HOUSE_SCRIPT(),
                0xFD  => new CM_TARGET_SELECT(conn, _world, _connRegistry),
                0xFE  => new CM_CHAT_MESSAGE_WHISPER(conn, _connRegistry),
                0x100 => new CM_EXCHANGE_ADD_KINAH(conn, _connRegistry, _exchangeService),
                0x101 => new CM_EXCHANGE_LOCK(conn, _connRegistry, _exchangeService, _itemDao),
                0x102 => new CM_EXCHANGE_ADD_ITEM(conn, _connRegistry, _exchangeService),
                0x105 => new CM_PING_REQUEST(conn),
                0x106 => new CM_VIEW_PLAYER_DETAILS(conn, _world),
                0x109 => new CM_CLIENT_COMMAND_ROLL(conn, _connRegistry),
                0x10C => new CM_MARK_FRIENDLIST(conn, _socialDao, _connRegistry),
                0x10D => new CM_FRIEND_ADD(conn, _playerDao, _socialDao, _connRegistry),
                0x10E => new CM_GROUP_DISTRIBUTION(conn, _connRegistry, _groupService),
                0x10F => new CM_UNK(),
                0x110 => new CM_HOUSE_EDIT(),
                0x112 => new CM_DELETE_QUEST(conn, _questDao),
                0x113 => new CM_PLAY_MOVIE_END(),
                0x114 => new CM_DIALOG_SELECT(conn, _world, _dataManager, _questDao, _itemDao, _expService, _connRegistry, _loggerFactory.CreateLogger<CM_DIALOG_SELECT>()),
                0x115 => new CM_LEGION_TABS(),
                0x116 => new CM_SHOW_DIALOG(conn, _world, _dataManager, _playerDao),
                0x117 => new CM_CLOSE_DIALOG(conn, _world),
                0x118 => new CM_SET_NOTE(conn, _playerDao, _socialDao, _connRegistry),
                0x119 => new CM_LEGION_MODIFY_EMBLEM(),
                0x11D => new CM_EXCHANGE_REQUEST(conn, _world, _connRegistry, _exchangeService),
                0x11E => new CM_GM_BOOKMARK(),
                0x11F => new CM_CHAT_GROUP_INFO(),
                0x122 => new CM_PLAYER_STATUS_INFO(conn, _connRegistry, _groupService),
                0x123 => new CM_INVITE_TO_GROUP(conn, _connRegistry, _groupService),
                0x124 => new CM_READ_MAIL(conn, _mailDao),
                0x126 => new CM_SEND_MAIL(conn, _playerDao, _mailDao, _itemDao, _connRegistry),
                0x127 => new CM_CHECK_MAIL_SIZE(),
                0x12A => new CM_GET_MAIL_ATTACHMENT(conn, _mailDao, _itemDao),
                0x12B => new CM_DELETE_MAIL(conn, _mailDao),
                0x12C => new CM_CLIENT_COMMAND_LOC(conn),
                0x12F => new CM_CRAFT(conn, _itemDao, _dataManager),
                0x129 => new CM_TITLE_SET(conn, _playerDao),
                0x130 => new CM_DUEL_REQUEST(conn, _connRegistry, _duelService, _world),
                0x132 => new CM_FRIEND_DEL(conn, _socialDao),
                0x136 => new CM_STOP_TRAINING(),
                0x138 => new CM_ITEM_REMODEL(),
                0x139 => new CM_GODSTONE_SOCKET(),
                0x13A => new CM_BUY_TRADE_IN_TRADE(),
                0x13B => new CM_RECIPE_DELETE(conn),
                0x13D => new CM_HOUSE_TELEPORT_BACK(),
                0x13E => new CM_FAST_TRACK(),
                0x140 => new CM_BROKER_SETTLE_ACCOUNT(),
                0x142 => new CM_BROKER_CANCEL_REGISTERED(),
                0x143 => new CM_BROKER_SETTLE_LIST(),
                0x144 => new CM_BLOCK_ADD(conn, _playerDao, _socialDao),
                0x145 => new CM_BLOCK_DEL(conn, _socialDao),
                0x146 => new CM_QUEST_SHARE(conn, _connRegistry, _dataManager),
                0x148 => new CM_FRIEND_STATUS(conn, _socialDao, _connRegistry),
                0x14C => new CM_CHAT_AUTH(conn, _csHolder),
                0x14D => new CM_MACRO_CREATE(conn, _macroDao),
                0x14E => new CM_CHANGE_CHANNEL(),
                0x153 => new CM_QUESTIONNAIRE(),
                0x154 => new CM_ABYSS_RANKING_LEGIONS(),
                0x155 => new CM_PRIVATE_STORE(),
                0x156 => new CM_DELETE_ITEM(conn, _itemDao),
                0x159 => new CM_BROKER_LIST(),
                0x15A => new CM_PRIVATE_STORE_NAME(),
                0x15B => new CM_SUMMON_COMMAND(),
                0x15C => new CM_BUY_BROKER_ITEM(),
                0x15D => new CM_REGISTER_BROKER_ITEM(),
                0x15E => new CM_BROKER_SEARCH(),
                0x15F => new CM_BROKER_REGISTERED(),
                0x160 => new CM_READ_EXPRESS_MAIL(),
                0x161 => new CM_SUBZONE_CHANGE(),
                0x162 => new CM_LEGION_UPLOAD_INFO(),
                0x163 => new CM_LEGION_UPLOAD_EMBLEM(),
                0x166 => new CM_SHOW_MAP(),
                0x167 => new CM_APPEARANCE(),
                0x168 => new CM_SUMMON_EMOTION(),
                0x169 => new CM_SUMMON_ATTACK(),
                0x16A => new CM_AUTO_GROUP(),
                0x16B => new CM_SUMMON_MOVE(),
                0x16C => new CM_FUSION_WEAPONS(),
                0x16D => new CM_BREAK_WEAPONS(),
                0x16F => new CM_SUMMON_CASTSPELL(),
                0x170 => new CM_REPLACE_ITEM(),
                0x171 => new CM_BLOCK_SET_REASON(conn, _socialDao),
                0x172 => new CM_MACRO_DELETE(conn, _macroDao),
                0x176 => new CM_TELEPORT_SELECT(conn, _world, _dataManager, _itemDao, _connRegistry),
                0x17C => new CM_SHOW_BLOCKLIST(conn, _socialDao),
                0x17D => new CM_PLAYER_SEARCH(conn, _connRegistry),
                0x17E => new CM_MOVE_ITEM(conn, _itemDao),
                0x17F => new CM_SPLIT_ITEM(conn, _itemDao, _dataManager),
                0x178 => new CM_START_LOOT(conn, _lootService),
                0x179 => new CM_LOOT_ITEM(conn, _lootService, _itemDao, _connRegistry),
                0x182 => new CM_INSTANCE_INFO(),
                0x183 => new CM_IN_GAME_SHOP_INFO(),
                0x184 => new CM_SHOW_FRIENDLIST(conn, _socialDao, _connRegistry),
                0x187 => new CM_SECURITY_TOKEN(),
                0x188 => new CM_USE_CHARGE_SKILL(),
                0x18A => new CM_CHALLENGE_LIST(),
                0x18B => new CM_BONUS_TITLE(),
                0x18E => new CM_SELECTITEM_OK(),
                0x192 => new CM_COMPOSITE_STONES(),
                0x197 => new CM_SHOW_BRAND(),
                0x19A => new CM_GROUP_LOOT(),
                0x19B => new CM_DISTRIBUTION_SETTINGS(),
                0x19D => new CM_REPORT_PLAYER(),
                0x19E => new CM_ABYSS_RANKING_PLAYERS(),
                0x19F => new CM_MAC_ADDRESS(),
                0x1A0 => new CM_HOUSE_OPEN_DOOR(),
                0x1A2 => new CM_USE_HOUSE_OBJECT(),
                0x1A3 => new CM_RELEASE_OBJECT(),
                0x1B2 => new CM_REGISTER_HOUSE(),
                0x1B4 => new CM_MEGAPHONE(),
                0x1B7 => new CM_CHECK_MAIL_SIZE2(),
                0x1B8 => new CM_GET_HOUSE_BIDS(),
                0x1B9 => new CM_FAST_TRACK_CHECK(),
                0x1BC => new CM_HOUSE_TELEPORT(),
                0x1BD => new CM_HOUSE_PAY_RENT(),
                0x1BF => new CM_PLACE_BID(),
                0x2E4 => new CM_WINDSTREAM(),
                0x2E5 => new CM_MOTION(),
                0x2E6 => new CM_EXCHANGE_OK(conn, _connRegistry, _exchangeService, _itemDao),
                0x2E7 => new CM_EXCHANGE_CANCEL(conn, _connRegistry, _exchangeService),
                0x2E8 => new CM_MANASTONE(conn, _itemDao),
                0x2E9 => new CM_HOUSE_DECORATE(),
                0x2EA => new CM_HOUSE_KICK(),
                0x2EB => new CM_HOUSE_SETTINGS(),
                0x2EC => new CM_CHARGE_ITEM(),
                0x2ED => new CM_GROUP_DATA_EXCHANGE(),
                0x2EE => new CM_LEGION_WH_KINAH(),
                0x2EF => new CM_FIND_GROUP(),
                _     => Unknown(state, opcode),
            },
            _ => Unknown(state, opcode),
        };
    }

    private AionClientPacket? Unknown(GsClientConnection.AionState state, ushort opcode)
    {
        _log.LogDebug("Unknown packet: state={State} opcode=0x{Op:X4}", state, opcode);
        return null;
    }
}
