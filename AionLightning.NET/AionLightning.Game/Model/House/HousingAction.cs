namespace AionLightning.Game.Model.House;

/// <summary>Java model.gameobjects.HousingAction — the action-id byte CM_HOUSE_EDIT reads first.</summary>
public enum HousingAction
{
    Unk = -1,
    EnterDecoration = 1,
    ExitDecoration = 2,
    AddItem = 3,
    DeleteItem = 4,
    SpawnObject = 5,
    MoveObject = 6,
    DespawnObject = 7,
    EnterRenovation = 14,
    ExitRenovation = 15,
    ChangeAppearance = 16,
}

public static class HousingActionExtensions
{
    /// <summary>Java HousingAction.getActionTypeById(int).</summary>
    public static HousingAction GetActionTypeById(int id) =>
        Enum.IsDefined(typeof(HousingAction), id) ? (HousingAction)id : HousingAction.Unk;
}
