namespace AionLightning.Game.Services.Siege;

/// <summary>Java services.siegeservice.SiegeException — thrown for siege lifecycle programming errors
/// (double start/stop already logged and swallowed by the caller instead; this is reserved for cases
/// Java itself let propagate, e.g. a missing/duplicate siege boss NPC or an unknown siege location id).</summary>
public sealed class SiegeException : Exception
{
    public SiegeException()
    {
    }

    public SiegeException(string message) : base(message)
    {
    }

    public SiegeException(string message, Exception inner) : base(message, inner)
    {
    }
}
