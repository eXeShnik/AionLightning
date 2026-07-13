namespace AionLightning.Game.Instance;

/// <summary>
/// Binds an <see cref="GeneralInstanceHandler"/> subclass to a world/map id (Java
/// <c>@InstanceID(worldId)</c>). The instance-handler engine reflects over compiled scripts and
/// registers each annotated class as the handler factory for its map.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class InstanceIdAttribute(int worldId) : Attribute
{
    public int WorldId { get; } = worldId;
}
