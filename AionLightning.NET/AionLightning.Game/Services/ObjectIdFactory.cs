namespace AionLightning.Game.Services;

public static class ObjectIdFactory
{
    // NPC IDs start well above typical player object IDs from the database
    private static int _next = 500_000_000;

    public static int Next() => Interlocked.Increment(ref _next);
}
