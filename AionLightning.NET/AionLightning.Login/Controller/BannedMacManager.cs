using AionLightning.Login.Model.Base;
using System.Collections.Generic;

namespace AionLightning.Login.Controller;

/// <summary>TODO M3: wire DAO via DI. Stub holds empty in-memory map.</summary>
public sealed class BannedMacManager
{
    private static readonly BannedMacManager _instance = new();
    private readonly Dictionary<string, BannedMacEntry> _banned = new();

    public static BannedMacManager GetInstance() => _instance;

    public bool IsBanned(string address)
    {
        if (!_banned.TryGetValue(address, out var entry)) return false;
        if (entry.TimeEnd > DateTime.UtcNow) return true;
        _banned.Remove(address);
        return false;
    }

    public Dictionary<string, BannedMacEntry> GetMap() => _banned;
}
