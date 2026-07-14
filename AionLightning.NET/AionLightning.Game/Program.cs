using AionLightning.Commons.Configuration;
using AionLightning.Commons.Database;
using AionLightning.Commons.Events;
using AionLightning.Commons.Hosting;
using AionLightning.Commons.Network;
using AionLightning.Commons.Scripting;
using AionLightning.Commons.Services;
using AionLightning.Game;
using AionLightning.Game.Combat.Handlers;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Events;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Cs;
using AionLightning.Game.Network.Ls;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GameWorld = AionLightning.Game.World.World;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;
using Microsoft.Extensions.Hosting;

var builder = AionHostBuilder.CreateAion(args, "AionGame");

// Options
builder.Services
    .AddAionOptions<NetworkOptions>("GameServer:Network")
    .AddAionOptions<LsConnectionOptions>("GameServer:LoginServer")
    .AddAionOptions<CsConnectionOptions>("GameServer:ChatServer")
    .AddAionOptions<GameServerInfoOptions>("GameServer:Info")
    .AddAionOptions<WorldOptions>("GameServer:World")
    .AddAionOptions<RateOptions>("GameServer:Rates")
    .AddAionOptions<GsOptions>("GameServer:Gs")
    .AddAionOptions<GeoDataOptions>("GameServer:GeoData")
    .AddAionOptions<SiegeOptions>("GameServer:Siege")
    .AddAionOptions<SiegeScheduleOptions>("GameServer:Siege:Schedule")
    .AddAionOptions<RiftOptions>("GameServer:Rift")
    .AddAionOptions<RiftScheduleOptions>("GameServer:Rift:Schedule")
    .AddAionOptions<HousingOptions>("GameServer:Housing")
    .AddAionOptions<HousingAuctionOptions>("GameServer:Housing:Auction")
    .AddAionOptions<DoorOptions>("GameServer:Doors")
    .AddAionOptions<FallDamageOptions>("GameServer:FallDamage");

// Database
builder.Services.AddAionDataSource(builder.Configuration, "GameDb");

// Event bus
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// M260+: damage observer handlers
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, HealCastorOnAttackedHandler>();
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, MagicCounterAtkHandler>();
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, ReflectorHandler>();
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, ConvertHealHandler>();
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, ProvokerHandler>();
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, CaseHealHandler>();
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, ChangeHateOnAttackedHandler>(); // M376
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, HpUpdateHandler>(); // C1: SM_STATUPDATE_HP to damaged players
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, SiegeBossDamageHandler>(); // siege boss damage feeds SiegeCounter
// M287: death event handlers
builder.Services.AddTransient<IEventHandler<DeathEvent>, HealCastorOnTargetDeadHandler>();
builder.Services.AddTransient<IEventHandler<DeathEvent>, ResurrectBaseHandler>(); // M379
builder.Services.AddTransient<IEventHandler<DeathEvent>, PvpKillHandler>(); // PvP Phase 1 — after ResurrectBaseHandler so a Chain-of-Suffering revive is visible via IsAlreadyDead
builder.Services.AddTransient<IEventHandler<DeathEvent>, InstanceDeathHandler>(); // instance script onDie(Npc)/onDie(Player)
builder.Services.AddTransient<IEventHandler<DeathEvent>, QuestPlayerDeathHandler>(); // quest onDie(player)
builder.Services.AddTransient<IEventHandler<DeathEvent>, SiegeBossDeathHandler>(); // siege boss death ends the siege
// Phase 5 Batch 0: bridges CM_LEVEL_READY's PlayerEnteredWorldEvent into QuestEngine.OnEnterWorldAsync
builder.Services.AddTransient<IEventHandler<PlayerEnteredWorldEvent>, QuestEnterWorldHandler>();
// M265+: pre-damage handlers (mutate MutableDamage to absorb)
builder.Services.AddTransient<IEventHandler<DamageReceivingEvent>, SanctuaryHandler>(); // sanctuary first — full immunity short-circuits everything else
builder.Services.AddTransient<IEventHandler<DamageReceivingEvent>, AlwaysResistHandler>(); // M276 — magic-only immunity, runs before partial-absorb handlers
builder.Services.AddTransient<IEventHandler<DamageReceivingEvent>, AlwaysBlockDodgeHandler>(); // M313 — physical block/dodge guarantee (hit counter)
builder.Services.AddTransient<IEventHandler<DamageReceivingEvent>, ShieldHandler>();
builder.Services.AddTransient<IEventHandler<DamageReceivingEvent>, MpShieldHandler>();
builder.Services.AddTransient<IEventHandler<DamageReceivingEvent>, ProtectHandler>();

// World
builder.Services.AddSingleton<GameWorld>();

// Geodata: real engine when GeoData:Enable=true (config default false — no .geo dataset ships
// with this repo yet, see migration_plan.md C4 survey), Dummy = Java-with-geo-disabled parity.
// The load-at-startup hosted service is only registered when the real engine is selected.
var geoDataEnabled = builder.Configuration.GetValue<bool>("GameServer:GeoData:Enable");
builder.Services.AddSingleton<AionLightning.Game.World.Geo.RealGeoService>();
builder.Services.AddSingleton<AionLightning.Game.World.Geo.IGeoService>(sp => geoDataEnabled
    ? sp.GetRequiredService<AionLightning.Game.World.Geo.RealGeoService>()
    : new AionLightning.Game.World.Geo.DummyGeoService());
if (geoDataEnabled)
    builder.Services.AddHostedService<AionLightning.Game.World.Geo.GeoLoadHostedService>();

// Data
builder.Services.AddSingleton<IDataManager, DataManager>();

// DAOs
builder.Services.AddSingleton<IPlayerDao, PlayerDaoImpl>();
builder.Services.AddSingleton<IPlayerAppearanceDao, PlayerAppearanceDaoImpl>();
builder.Services.AddSingleton<IItemDao, ItemDaoImpl>();
builder.Services.AddSingleton<ISocialDao, SocialDaoImpl>();
builder.Services.AddSingleton<IMailDao, MailDaoImpl>();
builder.Services.AddSingleton<IQuestDao, QuestDaoImpl>();
builder.Services.AddSingleton<IMacroDao, MacroDaoImpl>();
builder.Services.AddSingleton<ILegionDao, LegionDaoImpl>();
builder.Services.AddSingleton<IPlayerSettingsDao, PlayerSettingsDaoImpl>();
builder.Services.AddSingleton<IRecipeDao, RecipeDaoImpl>();
builder.Services.AddSingleton<IMotionDao, MotionDaoImpl>();
builder.Services.AddSingleton<ISkillDao, SkillDaoImpl>();
builder.Services.AddSingleton<IBrokerDao, BrokerDaoImpl>();
builder.Services.AddSingleton<IManastoneDao, ManastoneDaoImpl>();
builder.Services.AddSingleton<IPlayerTitleDao, PlayerTitleDaoImpl>();
builder.Services.AddSingleton<IPetDao, PetDaoImpl>();
builder.Services.AddSingleton<IHouseDao, HouseDaoImpl>();
builder.Services.AddSingleton<IHouseBidsDao, HouseBidsDaoImpl>();
builder.Services.AddSingleton<IHouseObjectCooldownsDao, HouseObjectCooldownsDaoImpl>();
builder.Services.AddSingleton<IPlayerRegisteredItemsDao, PlayerRegisteredItemsDaoImpl>();
builder.Services.AddSingleton<IHouseScriptsDao, HouseScriptsDaoImpl>();
builder.Services.AddSingleton<ISiegeDao, SiegeDaoImpl>();

// Scripting
builder.Services.AddSingleton<CSharpCompilerService>();
builder.Services.AddSingleton<ScriptService>();

// Connection holders
builder.Services.AddSingleton<LsConnectionHolder>();
builder.Services.AddSingleton<CsConnectionHolder>();

// Packet factories
builder.Services.AddSingleton<LsPacketHandlerFactory>();
builder.Services.AddSingleton<CsPacketHandlerFactory>();
builder.Services.AddSingleton<GsPacketHandlerFactory>();

// Services
builder.Services.AddSingleton<SpawnService>();
builder.Services.AddSingleton<SkillLearnService>();
builder.Services.AddSingleton<StigmaService>();
builder.Services.AddSingleton<ClassChangeService>();
builder.Services.AddSingleton<ExperienceService>();
builder.Services.AddSingleton<LootService>();
builder.Services.AddSingleton<ExchangeService>();
builder.Services.AddSingleton<GroupService>();
builder.Services.AddSingleton<AllianceService>();
builder.Services.AddSingleton<LeagueService>();
builder.Services.AddSingleton<QuestService>();
builder.Services.AddSingleton<QuestRewardService>();
builder.Services.AddSingleton<QuestEngineType>();
builder.Services.AddSingleton<DuelService>();
builder.Services.AddSingleton<LegionService>();
builder.Services.AddSingleton<GatherService>();
builder.Services.AddSingleton<BrokerService>();
builder.Services.AddSingleton<RepurchaseService>();
builder.Services.AddSingleton<PrivateStoreService>();
builder.Services.AddSingleton<FindGroupService>();
builder.Services.AddSingleton<SummonsService>();
builder.Services.AddSingleton<ZoneService>();
builder.Services.AddSingleton<FallDamageService>();
builder.Services.AddSingleton<KiskService>();
// Instance subsystem: channel registry + lifecycle + teleport choke point + scriptable handlers
builder.Services.AddSingleton<AionLightning.Game.World.InstanceRegistry>();
builder.Services.AddSingleton<AionLightning.Game.Instance.InstanceEngine>();
builder.Services.AddSingleton<AionLightning.Game.Ai.AiEngine>();
builder.Services.AddSingleton<InstanceService>();
builder.Services.AddSingleton<TeleportService>();
builder.Services.AddSingleton<PortalService>();
builder.Services.AddSingleton<PetService>();
builder.Services.AddSingleton<AionLightning.Game.Model.Siege.Influence>();
builder.Services.AddSingleton<AionLightning.Game.Services.Siege.Assault.BalaurAssaultService>();
builder.Services.AddSingleton<SiegeService>();
builder.Services.AddSingleton<RiftService>();
builder.Services.AddSingleton<HousingService>();
builder.Services.AddSingleton<HousingBidService>();
builder.Services.AddSingleton<AionLightning.Game.Controllers.HouseController>();
builder.Services.AddSingleton<AionLightning.Game.Controllers.FlyController>();
builder.Services.AddSingleton<MaintenanceTask>();
builder.Services.AddSingleton<DoorService>();
builder.Services.AddSingleton<TribeRelationService>();
builder.Services.AddSingleton<NpcShoutsService>();
builder.Services.AddSingleton<FollowService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<FollowService>());
builder.Services.AddHostedService<EmptyInstanceCheckerService>();
builder.Services.AddHostedService<RegenService>();
builder.Services.AddSingleton<NpcAiService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NpcAiService>());
builder.Services.AddSingleton<EffectTickScheduler>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EffectTickScheduler>());
builder.Services.AddSingleton<AuraChildApplier>();
builder.Services.AddSingleton<PlayerEnterWorldService>();
builder.Services.AddHostedService<AutoSaveService>();
builder.Services.AddHostedService<AbyssResetService>();
builder.Services.AddSingleton<CronService>();
builder.Services.AddHostedService<CronServiceHostedService>();

// System mail (siege rewards, housing auction/maintenance)
builder.Services.AddSingleton<AionLightning.Game.Services.Mail.SystemMailService>();
builder.Services.AddSingleton<AionLightning.Game.Services.Mail.MailFormatter>();

// Account registry (TCS bridge for LS auth roundtrip)
builder.Services.AddSingleton<GameAccountRegistry>();
builder.Services.AddSingleton<ReconnectRegistry>();
builder.Services.AddSingleton<PlayerResponseRegistry>();

// Maps online player objectId → connection for broadcast
builder.Services.AddSingleton<PlayerConnectionRegistry>();

// Aion-client connection factory
builder.Services.AddSingleton<IConnectionFactory<GsClientConnection>, GsConnectionFactory>();

// Hosted services: schema migration first, then quest engine bootstrap, then server
builder.Services.AddHostedService<SchemaMigrationHost>();
builder.Services.AddHostedService<SiegeServiceHostedService>();
builder.Services.AddHostedService<RiftServiceHostedService>();
builder.Services.AddHostedService<HousingServiceHostedService>();
builder.Services.AddHostedService<HousingBidServiceHostedService>();
builder.Services.AddHostedService<MaintenanceTaskHostedService>();
builder.Services.AddHostedService<QuestEngineHostedService>();
builder.Services.AddHostedService<AionLightning.Game.Instance.InstanceEngineHostedService>();
builder.Services.AddHostedService<AionLightning.Game.Ai.AiEngineHostedService>();
builder.Services.AddHostedService<GameServerHost>();

await builder.Build().RunAsync();
