using AionLightning.Game.Model.Templates.Npc;

namespace AionLightning.Game.Model;

public sealed class Npc : Creature
{
    public NpcTemplate Template { get; }

    public byte Level => Template.Level;

    /// <summary>The position at which this NPC was originally spawned; used for leash range checks.</summary>
    public Position HomePosition { get; set; }

    public Npc(NpcTemplate template)
    {
        Template  = template;
        Name      = template.Name;
        MaxHp     = template.MaxHp;
        CurrentHp = template.MaxHp;
    }
}
