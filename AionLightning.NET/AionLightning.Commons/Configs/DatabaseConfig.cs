namespace AionLightning.Commons.Configs
{
    public class DatabaseConfig
    {
        // TODO: Load these settings from IConfiguration
        public static string DATABASE_URL = "Server=localhost;Database=aion;Uid=root;Pwd=;";
        public static string DATABASE_DRIVER = "MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data";
        public static string DATABASE_USER = "root";
        public static string DATABASE_PASSWORD = "";
        public static int DATABASE_BONECP_PARTITION_COUNT = 1;
        public static int DATABASE_BONECP_PARTITION_CONNECTIONS_MIN = 5;
        public static int DATABASE_BONECP_PARTITION_CONNECTIONS_MAX = 10;
    }
}
