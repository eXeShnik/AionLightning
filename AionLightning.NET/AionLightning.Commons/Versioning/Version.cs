using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace AionLightning.Commons.Versioning
{
    public class Locator
    {
        public static FileInfo GetClassSource(Type c)
        {
            return new FileInfo(c.Assembly.Location);
        }
    }

    public class Version
    {
        private readonly ILogger<Version> _log;

        public string Revision { get; private set; }
        public string Date { get; private set; }
        public string Branch { get; private set; }
        public string CommitTime { get; private set; }

        public Version(ILogger<Version> log)
        {
            _log = log;
        }

        public Version(Type c, ILogger<Version> log) : this(log)
        {
            LoadInformation(c);
        }

        public void LoadInformation(Type c)
        {
            try
            {
                var assembly = c.Assembly;
                var attributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();

                foreach (var attribute in attributes)
                {
                    switch (attribute.Key)
                    {
                        case "Revision":
                            Revision = attribute.Value;
                            break;
                        case "Date":
                            Date = attribute.Value;
                            break;
                        case "Branch":
                            Branch = attribute.Value;
                            break;
                        case "CommitTime":
                            CommitTime = attribute.Value;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.LogError(e, "Unable to get software information");
            }
        }

        public override string ToString()
        {
            return $"Revision: {Revision} Build date: {Date} Branch: {Branch}";
        }
    }
}
