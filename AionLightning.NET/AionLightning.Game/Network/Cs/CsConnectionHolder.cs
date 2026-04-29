namespace AionLightning.Game.Network.Cs;

public sealed class CsConnectionHolder
{
    private volatile CsConnection? _current;
    public CsConnection? Current { get => _current; set => _current = value; }
}
