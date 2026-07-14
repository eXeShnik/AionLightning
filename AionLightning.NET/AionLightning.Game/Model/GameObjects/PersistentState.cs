namespace AionLightning.Game.Model.GameObjects;

/// <summary>
/// Java model.gameobjects.PersistentState — per-row DB dirty-tracking for a placed <see cref="HouseObject"/>
/// or custom <see cref="HouseDecoration"/>. Java also keeps a registry-level aggregate UPDATE_REQUIRED flag
/// so periodic saves can skip untouched registries; this port has no periodic housing save task — CM
/// handlers persist the row(s) they touched immediately (see HouseRegistry's Set/Put/Remove methods), so
/// only the per-item transitions below are needed.
/// </summary>
public enum PersistentState
{
    /// <summary>Created this session, never written to the DB yet.</summary>
    New,
    /// <summary>Already has a DB row; a field changed and the row needs a re-save.</summary>
    UpdateRequired,
    /// <summary>Matches the DB row as of the last save/load.</summary>
    Updated,
    /// <summary>Has a DB row that must be deleted.</summary>
    Deleted,
    /// <summary>Never persisted and never will be (default building-part decorations).</summary>
    NoAction,
}
