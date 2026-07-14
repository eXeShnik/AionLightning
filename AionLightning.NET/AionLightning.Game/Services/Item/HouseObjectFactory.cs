using AionLightning.Game.DataHolders;
using HouseModel = AionLightning.Game.Model.House.House;
using AionLightning.Game.Model.GameObjects;
using AionLightning.Game.Model.Templates.Item;

// Deliberately flat "AionLightning.Game.Services" rather than "...Services.Item" — a nested Item
// namespace here would shadow the "using AionLightning.Game.Model.Item;" directive used by other files
// in this same Services namespace tree (EquipStatsCalculator/ExchangeService/PetService all reference the
// Model.Item.Item class unqualified), the same enclosing-namespace-wins-over-using-directive rule that
// requires HouseModel's alias below. File still lives under Services/Item/ to mirror Java's
// services.item package layout.
namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java services.item.HouseObjectFactory. Java dispatches to one of ten HouseObject subclasses by
/// template type; this port always builds the single concrete <see cref="Model.GameObjects.HouseObject"/>
/// class instead (see its class doc) — <see cref="Model.Templates.Housing.HousingObjectTemplate.Kind"/>
/// still drives the SM_HOUSE_OBJECT/SM_HOUSE_EDIT wire tail. Static utility — every dependency is passed
/// in by the caller (no held DI state), matching how <see cref="ObjectIdFactory"/> is already
/// called directly from HousingService.
/// </summary>
public static class HouseObjectFactory
{
    /// <summary>Java HouseObjectFactory.createNew(House, int, int) — for loading a placed object back from
    /// the DB (an already-known objectId/templateId pair). Returns null when the template can't be
    /// resolved (housing_objects.xml missing the id — caller should skip/log, not crash).</summary>
    public static HouseObject? CreateNew(HouseModel house, int objectId, int templateId, IDataManager dataManager)
    {
        var template = dataManager.HousingObjects.GetTemplateById(templateId);
        if (template is null) return null;
        return new HouseObject(house, objectId, templateId, template);
    }

    /// <summary>Java HouseObjectFactory.createNew(House, ItemTemplate) — turns an inventory item carrying a
    /// &lt;houseobject id="..."/&gt; action into a brand-new placed-object registry entry (objectId minted
    /// via <see cref="Services.ObjectIdFactory"/>, matching Java's IDFactory.nextId() call here). Returns
    /// null when the item has no house-object action or the action's template can't be resolved.</summary>
    public static HouseObject? CreateFromItem(HouseModel house, ItemTemplate itemTemplate, IDataManager dataManager)
    {
        var action = itemTemplate.Actions?.HouseObject;
        if (action is null) return null;

        int objectId = ObjectIdFactory.Next();
        var obj = CreateNew(house, objectId, action.TemplateId, dataManager);
        if (obj?.Template is { UseDays: > 0 } template)
            obj.ExpireEnd = (int)DateTimeOffset.UtcNow.AddDays(template.UseDays).ToUnixTimeSeconds();
        return obj;
    }
}
