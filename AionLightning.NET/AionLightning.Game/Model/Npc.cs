using AionLightning.Game.Model.Templates.Npc;

namespace AionLightning.Game.Model;

public sealed class Npc : Creature
{
    public NpcTemplate Template { get; }

    public Npc(NpcTemplate template)
    {
        Template  = template;
        MaxHp     = template.MaxHp;
        CurrentHp = template.MaxHp;
    }
}
