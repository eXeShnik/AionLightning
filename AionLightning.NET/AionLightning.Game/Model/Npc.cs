using AionLightning.Game.Model.Templates.Npc;

namespace AionLightning.Game.Model;

public sealed class Npc : Creature
{
    public NpcTemplate Template { get; }

    public byte Level => Template.Level;

    /// <summary>The position at which this NPC was originally spawned; used for leash range checks.</summary>
    public Position HomePosition { get; set; }

    /// <summary>Seconds until this NPC respawns after death. 0 means use the service default.</summary>
    public int RespawnTime { get; set; }

    /// <summary>Route ID from npc_walker.xml. Empty string means random wander; set from spawn spot data.</summary>
    public string WalkerId { get; set; } = string.Empty;

    public Npc(NpcTemplate template)
    {
        Template       = template;
        Name           = template.Name;
        MaxHp          = template.MaxHp;
        CurrentHp      = template.MaxHp;
        MovementSpeed  = template.Stats?.RunSpeed ?? 6.0f;
    }
}
