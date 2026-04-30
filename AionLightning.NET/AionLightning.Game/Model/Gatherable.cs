using AionLightning.Game.Model.Templates.Gatherable;

namespace AionLightning.Game.Model;

public sealed class Gatherable : VisibleObject
{
    public GatherableTemplate Template  { get; }
    public Position           HomePosition { get; init; }
    public int                HarvestsRemaining { get; set; }
    public bool               IsGathered => HarvestsRemaining <= 0;

    public Gatherable(GatherableTemplate template)
    {
        Template          = template;
        HarvestsRemaining = template.HarvestCount;
    }
}
