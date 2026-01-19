using AionLightning.Commons.Configuration;

namespace AionLightning.Login.Configs
{
    public class SvStatsConfig
    {
        [Property(Key = "svstats.enable_svstats", DefaultValue = "false")]
        public static bool SVSTATS_ENABLE;
    }
}
