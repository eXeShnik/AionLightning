namespace AionLightning.Game.Network.Ls;

/// <summary>
/// Singleton holder for the current LS connection.
/// Replaced on each reconnect; may be null when disconnected.
/// </summary>
public sealed class LsConnectionHolder
{
    private volatile LsConnection? _current;
    public LsConnection? Current { get => _current; set => _current = value; }
}
