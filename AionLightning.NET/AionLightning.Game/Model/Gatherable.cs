using AionLightning.Game.Model.Templates.Gatherable;

namespace AionLightning.Game.Model;

public sealed class Gatherable : VisibleObject
{
    public GatherableTemplate Template  { get; }
    public Position           HomePosition { get; init; }
    public int                HarvestsRemaining { get; set; }
    public bool               IsGathered => HarvestsRemaining <= 0;

    /// <summary>Seconds until this node respawns after being depleted. 0 means use the service default.</summary>
    public int RespawnTime { get; set; }

    public Gatherable(GatherableTemplate template)
    {
        Template          = template;
        HarvestsRemaining = template.HarvestCount;
    }
}
