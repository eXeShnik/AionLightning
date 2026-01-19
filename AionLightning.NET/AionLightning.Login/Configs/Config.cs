using Microsoft.Extensions.Configuration;
using System.IO;

namespace AionLightning.LoginServer.Configs
{
    public static class Config
    {
        private static IConfigurationRoot _configuration;

        public static string ACCOUNT_CHARSET { get; private set; }
        public static int FAST_RECONNECTION_TIME { get; private set; }
        public static int LOGIN_PORT { get; private set; }
        public static string LOGIN_BIND_ADDRESS { get; private set; }
        public static int GAME_PORT { get; private set; }
        public static string GAME_BIND_ADDRESS { get; private set; }
        public static int LOGIN_TRY_BEFORE_BAN { get; private set; }
        public static int WRONG_LOGIN_BAN_TIME { get; private set; }
        public static int NIO_READ_THREADS { get; private set; }
        public static int NIO_WRITE_THREADS { get; private set; }
        public static bool ACCOUNT_AUTO_CREATION { get; private set; }
        public static bool MAINTENANCE_MOD { get; private set; }
        public static int MAINTENANCE_MOD_GMLEVEL { get; private set; }
        public static bool ENABLE_FLOOD_PROTECTION { get; private set; }
        public static bool ENABLE_BRUTEFORCE_PROTECTION { get; private set; }
        public static bool ENABLE_PINGPONG { get; private set; }
        public static int PINGPONG_DELAY { get; private set; }
        public static string EXCLUDED_IP { get; private set; }
        public static IConfiguration DATABASE_CONFIG { get; private set; }

        public static void Load()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();

            ACCOUNT_CHARSET = _configuration.GetValue<string>("accounts.charset", "ISO8859_2");
            FAST_RECONNECTION_TIME = _configuration.GetValue<int>("network.fastreconnection.time", 10);
            LOGIN_PORT = _configuration.GetValue<int>("loginserver.network.client.port", 2106);
            LOGIN_BIND_ADDRESS = _configuration.GetValue<string>("loginserver.network.client.host", "localhost");
            GAME_PORT = _configuration.GetValue<int>("loginserver.network.gameserver.port", 9014);
            GAME_BIND_ADDRESS = _configuration.GetValue<string>("loginserver.network.gameserver.host", "*");
            LOGIN_TRY_BEFORE_BAN = _configuration.GetValue<int>("loginserver.network.client.logintrybeforeban", 5);
            WRONG_LOGIN_BAN_TIME = _configuration.GetValue<int>("loginserver.network.client.bantimeforbruteforcing", 15);
            NIO_READ_THREADS = _configuration.GetValue<int>("loginserver.network.nio.threads.read", 0);
            NIO_WRITE_THREADS = _configuration.GetValue<int>("loginserver.network.nio.threads.write", 0);
            ACCOUNT_AUTO_CREATION = _configuration.GetValue<bool>("loginserver.accounts.autocreate", true);
            MAINTENANCE_MOD = _configuration.GetValue<bool>("loginserver.server.maintenance", false);
            MAINTENANCE_MOD_GMLEVEL = _configuration.GetValue<int>("loginserver.server.maintenance.gmlevel", 3);
            ENABLE_FLOOD_PROTECTION = _configuration.GetValue<bool>("loginserver.server.floodprotector", true);
            ENABLE_BRUTEFORCE_PROTECTION = _configuration.GetValue<bool>("loginserver.server.bruteforceprotector", true);
            ENABLE_PINGPONG = _configuration.GetValue<bool>("loginserver.server.pingpong", true);
            PINGPONG_DELAY = _configuration.GetValue<int>("loginserver.server.pingpong.delay", 3000);
            EXCLUDED_IP = _configuration.GetValue<string>("loginserver.excluded.ips", "");
            DATABASE_CONFIG = _configuration.GetSection("Database");
        }

        public static IConfiguration GetLoggingConfiguration()
        {
            return _configuration.GetSection("Serilog");
        }
    }
}
