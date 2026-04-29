namespace AionLightning.Game.Model;

public abstract class Creature : VisibleObject
{
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int MaxMp { get; set; }
    public int CurrentMp { get; set; }
}
