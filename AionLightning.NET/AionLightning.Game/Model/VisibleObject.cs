namespace AionLightning.Game.Model;

public abstract class VisibleObject
{
    public int ObjectId { get; set; }
    public string Name { get; init; } = string.Empty;
    public Position Position { get; set; }
}
