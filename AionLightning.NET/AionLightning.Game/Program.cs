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
using AionLightning.Game.Services;
using Microsoft.Extensions.DependencyInjection;
using GameWorld = AionLightning.Game.World.World;
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
    .AddAionOptions<GsOptions>("GameServer:Gs");

// Database
builder.Services.AddAionDataSource(builder.Configuration, "GameDb");

// Event bus
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// M260+: damage observer handlers
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, HealCastorOnAttackedHandler>();
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, MagicCounterAtkHandler>();
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, ReflectorHandler>();
builder.Services.AddTransient<IEventHandler<DamageDealtEvent>, ConvertHealHandler>();
// M265+: pre-damage handlers (mutate MutableDamage to absorb)
builder.Services.AddTransient<IEventHandler<DamageReceivingEvent>, ShieldHandler>();

// World
builder.Services.AddSingleton<GameWorld>();

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
builder.Services.AddSingleton<ExperienceService>();
builder.Services.AddSingleton<LootService>();
builder.Services.AddSingleton<ExchangeService>();
builder.Services.AddSingleton<GroupService>();
builder.Services.AddSingleton<QuestService>();
builder.Services.AddSingleton<DuelService>();
builder.Services.AddSingleton<LegionService>();
builder.Services.AddSingleton<GatherService>();
builder.Services.AddSingleton<BrokerService>();
builder.Services.AddSingleton<RepurchaseService>();
builder.Services.AddSingleton<FindGroupService>();
builder.Services.AddHostedService<RegenService>();
builder.Services.AddSingleton<NpcAiService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NpcAiService>());
builder.Services.AddSingleton<PlayerEnterWorldService>();
builder.Services.AddHostedService<AutoSaveService>();
builder.Services.AddHostedService<AbyssResetService>();

// Account registry (TCS bridge for LS auth roundtrip)
builder.Services.AddSingleton<GameAccountRegistry>();
builder.Services.AddSingleton<ReconnectRegistry>();
builder.Services.AddSingleton<PlayerResponseRegistry>();

// Maps online player objectId → connection for broadcast
builder.Services.AddSingleton<PlayerConnectionRegistry>();

// Aion-client connection factory
builder.Services.AddSingleton<IConnectionFactory<GsClientConnection>, GsConnectionFactory>();

// Hosted services: schema migration first, then server
builder.Services.AddHostedService<SchemaMigrationHost>();
builder.Services.AddHostedService<GameServerHost>();

await builder.Build().RunAsync();
