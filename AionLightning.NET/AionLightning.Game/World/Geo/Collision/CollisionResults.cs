namespace AionLightning.Game.World.Geo.Collision;

/// <summary>Port of the Java geoEngine <c>CollisionResults</c> — a lazily-sorted result bag.</summary>
internal sealed class CollisionResults(byte intentions, bool onlyFirst, int instanceId)
{
    private readonly List<CollisionResult> _results = [];
    private bool _sorted = true;

    public bool IsOnlyFirst => onlyFirst;
    public byte Intentions => intentions;
    public int InstanceId => instanceId;

    public int Size => _results.Count;

    public void AddCollision(CollisionResult result)
    {
        if (float.IsNaN(result.Distance))
            return;

        _results.Add(result);
        if (!onlyFirst)
            _sorted = false;
    }

    public CollisionResult? GetClosestCollision()
    {
        if (_results.Count == 0)
            return null;

        EnsureSorted();
        return _results[0];
    }

    public CollisionResult? GetFarthestCollision()
    {
        if (_results.Count == 0)
            return null;

        EnsureSorted();
        return _results[^1];
    }

    public CollisionResult GetCollisionDirect(int index) => _results[index];

    private void EnsureSorted()
    {
        if (_sorted)
            return;

        _results.Sort();
        _sorted = true;
    }
}
