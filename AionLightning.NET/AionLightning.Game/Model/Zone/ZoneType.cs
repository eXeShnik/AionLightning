namespace AionLightning.Game.Model.Zone;

/// <summary>
/// Java <c>model.templates.zone.ZoneType</c> — mirrors Java's enum values exactly. Only <see cref="Fly"/>
/// has a ported consumer so far (the CM_EMOTION flight gate + FP-drain-rate check); the others are
/// declared for parity but not yet set by any zone-membership system in this port.
/// </summary>
public enum ZoneType
{
    Fly    = 0,
    Damage = 1,
    Water  = 2,
    Siege  = 3,
    Pvp    = 4,
}
