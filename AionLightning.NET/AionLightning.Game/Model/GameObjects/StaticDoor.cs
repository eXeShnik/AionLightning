using TemplateType = AionLightning.Game.Model.Templates.StaticDoor.StaticDoorTemplate;
using DoorStateFlags = AionLightning.Game.Model.Templates.StaticDoor.StaticDoorState;

namespace AionLightning.Game.Model.GameObjects;

/// <summary>
/// Runtime static-door object (Java model.gameobjects.StaticDoor extends StaticObject). A door's mesh
/// is baked into the client's map geometry — the server only tracks open/closed state per spawned
/// door instance and broadcasts state changes; it never sends a spawn packet for one (Java's
/// StaticObjectController has no onSee/appear hook, unlike Npc/Gatherable).
/// note: Java also resolves a geo "door name" (GeoService.getDoorName) from the mesh file so the
/// geodata engine can toggle mesh collision on open/close. This port has no geodata engine yet
/// (migration_plan.md) so <see cref="Template"/>.MeshFile is carried but never resolved/toggled.
/// </summary>
public sealed class StaticDoor : VisibleObject
{
    public TemplateType Template { get; }

    /// <summary>Map-design id from the static-doors XML — unique per (worldId, instanceId) scope, NOT
    /// the object id. Java <c>WorldMapInstance.getDoors()</c> keys its per-channel door map by this id
    /// (<c>getSpawn().getStaticId()</c>); CM_OPEN_STATICDOOR's payload is this id too.</summary>
    public int StaticId => Template.DoorId;

    public DoorStateFlags States { get; private set; }

    public bool IsOpen => States.HasFlag(DoorStateFlags.Opened);

    public StaticDoor(TemplateType template)
    {
        Template = template;
        Name     = "door";
        States   = template.InitialStates;
    }

    /// <summary>Java StaticDoor.setOpen — flips the OPENED/CLICKABLE flags for this door. Unlike Java,
    /// this model does not broadcast itself; callers (see Services.DoorService) own packet delivery so
    /// the broadcast can be scope/config-gated in one place.</summary>
    public void SetOpen(bool open)
    {
        if (open)
        {
            States &= ~DoorStateFlags.Clickable;
            States |= DoorStateFlags.Opened;
        }
        else
        {
            if (Template.InitialStates.HasFlag(DoorStateFlags.Clickable))
                States |= DoorStateFlags.Clickable;
            States &= ~DoorStateFlags.Opened;
        }
    }

    /// <summary>Java StaticDoor.changeState — sets the low-nibble state flags directly (admin/debug tool
    /// path), independent of the open/close semantics <see cref="SetOpen"/> applies.</summary>
    public void ChangeState(int rawState) => States = (DoorStateFlags)(rawState & 0xF);
}
