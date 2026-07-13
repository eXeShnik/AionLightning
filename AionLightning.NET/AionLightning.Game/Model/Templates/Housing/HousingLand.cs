namespace AionLightning.Game.Model.Templates.Housing;

/// <summary>One &lt;land&gt; element from houses.xml (Java model.templates.housing.HousingLand) — a housing
/// district (e.g. a specific estate/mansion/studio area) grouping addresses, the buildings sellable on
/// them, sale pricing and the maintenance fee.</summary>
public sealed record HousingLand(
    int Id,
    int TeleportNpcId,
    int ManagerNpcId,
    int HomeSignNpcId,
    int WaitingSignNpcId,
    int SaleSignNpcId,
    int NosaleSignNpcId,
    IReadOnlyList<HouseAddress> Addresses,
    IReadOnlyList<Building> Buildings,
    Sale SaleOptions,
    long MaintenanceFee)
{
    /// <summary>The building marked default="true" in houses.xml, or the first building as a fallback
    /// (mirrors Java HousingLand.getDefaultBuilding()'s "fail" fallback to buildings.get(0)).</summary>
    public Building? DefaultBuilding => Buildings.FirstOrDefault(b => b.IsDefault) ?? Buildings.FirstOrDefault();
}
