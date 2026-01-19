using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;

namespace AionLightning.Commons.Utils
{
    public static class PropertiesUtils
    {
        public static IConfiguration Load(string file)
        {
            return Load(new FileInfo(file));
        }

        public static IConfiguration Load(FileInfo file)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile(file.FullName, optional: true, reloadOnChange: true);
            return builder.Build();
        }

        public static IEnumerable<IConfiguration> Load(params string[] files)
        {
            var configurations = new List<IConfiguration>();
            foreach (var file in files)
            {
                configurations.Add(Load(file));
            }
            return configurations;
        }

        public static IEnumerable<IConfiguration> Load(params FileInfo[] files)
        {
            var configurations = new List<IConfiguration>();
            foreach (var file in files)
            {
                configurations.Add(Load(file));
            }
            return configurations;
        }
    }
}
