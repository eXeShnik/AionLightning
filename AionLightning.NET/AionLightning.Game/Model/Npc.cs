using AionLightning.Game.Ai;
using AionLightning.Game.Model.Templates.Npc;
using AionLightning.Game.Model.Templates.Tribe;

namespace AionLightning.Game.Model;

public sealed class Npc : Creature
{
    public NpcTemplate Template { get; }

    /// <summary>Java <c>Npc.getTribe()</c> (transform-model override omitted — polymorph/transform isn't
    /// ported yet, see migration_plan.md). Parsed from the template's <c>tribe=""</c> XML attribute.</summary>
    public override TribeClass Tribe => new(Template.Tribe);

    /// <summary>The compiled per-NPC AI script bound to this instance's ai-name, or null when none is registered (see <see cref="Ai.AiEngine"/>).</summary>
    public NpcAi2? ScriptedAi { get; set; }

    public byte Level => Template.Level;

    /// <summary>The position at which this NPC was originally spawned; used for leash range checks.</summary>
    public Position HomePosition { get; set; }

    /// <summary>Seconds until this NPC respawns after death. 0 means use the service default.</summary>
    public int RespawnTime { get; set; }

    /// <summary>Route ID from npc_walker.xml. Empty string means random wander; set from spawn spot data.</summary>
    public string WalkerId { get; set; } = string.Empty;

    /// <summary>M279: hate accumulator keyed by attacker objectId. Drives target selection priority — highest hate wins.</summary>
    public Dictionary<int, int> HateList { get; } = new();

    /// <summary>Adds hate from <paramref name="attackerObjectId"/>. Negative amounts subtract.</summary>
    public void AddHate(int attackerObjectId, int amount)
    {
        if (attackerObjectId == 0 || amount == 0) return;
        if (HateList.TryGetValue(attackerObjectId, out int cur))
            HateList[attackerObjectId] = Math.Max(0, cur + amount);
        else if (amount > 0)
            HateList[attackerObjectId] = amount;
    }

    /// <summary>Returns the objectId with the highest hate, or 0 if list is empty.</summary>
    public int TopHateObjectId()
    {
        if (HateList.Count == 0) return 0;
        int top = 0, best = -1;
        foreach (var kv in HateList)
            if (kv.Value > best) { best = kv.Value; top = kv.Key; }
        return top;
    }

    public Npc(NpcTemplate template)
    {
        Template       = template;
        Name           = template.Name;
        MaxHp          = template.MaxHp;
        CurrentHp      = template.MaxHp;
        MovementSpeed  = template.Stats?.RunSpeed ?? 6.0f;
    }
}
