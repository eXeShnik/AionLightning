using System.Reflection;

namespace AionLightning.Commons.Versioning;

/// <summary>
/// Build/revision metadata reader.
/// Java equivalent: com.aionemu.commons.versionning.Version
/// Java read from JAR manifests; .NET reads from assembly attributes.
/// </summary>
public sealed class Version
{
    public string Revision   { get; private set; } = "Unknown";
    public string Date       { get; private set; } = "Unknown";
    public string Branch     { get; private set; } = "Unknown";
    public string CommitTime { get; private set; } = "Unknown";

    public Version() { }

    public Version(Type type) => LoadInformation(type.Assembly);

    public void LoadInformation(Assembly assembly)
    {
        // InformationalVersion is the richest source — typically set to a git describe string
        var infoVer = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (infoVer is not null)
            Revision = infoVer;
        else
            Revision = assembly.GetName().Version?.ToString() ?? "Unknown";

        // Optional per-project [assembly: AssemblyMetadata("Date", "...")] entries
        foreach (var meta in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            switch (meta.Key)
            {
                case "Date":       Date       = meta.Value ?? Date;       break;
                case "Branch":     Branch     = meta.Value ?? Branch;     break;
                case "CommitTime": CommitTime = meta.Value ?? CommitTime; break;
            }
        }
    }

    public override string ToString() =>
        $"Revision={Revision} Branch={Branch} Date={Date} CommitTime={CommitTime}";
}
