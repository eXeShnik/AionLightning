using System.Globalization;
using System.Xml;
using AionLightning.Game.Model.Templates.Housing;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Loads housing/houses.xml (lands, addresses, per-land building refs, sale options, maintenance fee)
/// and housing/house_buildings.xml (canonical building type/size/parts definitions) — Java
/// dataholders.HouseData + dataholders.HouseBuildingData combined into one holder.
/// </summary>
public sealed class HousingData
{
    private readonly Dictionary<int, HousingLand> _lands = new();
    private readonly Dictionary<int, HouseAddress> _addresses = new();
    private readonly Dictionary<int, Building> _buildingDefs = new();

    public IReadOnlyCollection<HousingLand> Lands => _lands.Values;

    public int Count => _lands.Count;

    public void Load(string dataRoot, ILogger log)
    {
        var buildingsPath = Path.Combine(dataRoot, "housing", "house_buildings.xml");
        var housesPath = Path.Combine(dataRoot, "housing", "houses.xml");

        LoadBuildingDefs(buildingsPath, log);
        LoadLands(housesPath, log);

        log.LogInformation("HousingData: loaded {Lands} lands, {Addresses} addresses, {Buildings} building templates",
            _lands.Count, _addresses.Count, _buildingDefs.Count);
    }

    public HousingLand? GetLand(int landId) => _lands.GetValueOrDefault(landId);

    public HouseAddress? GetAddress(int addressId) => _addresses.GetValueOrDefault(addressId);

    /// <summary>Resolves the land that owns a given address id. Java kept a bidirectional
    /// address-&gt;land reference on the loaded template graph; this holder only indexes addresses/lands
    /// separately, so the lookup is a linear scan over the (small, load-time-only) land set instead.</summary>
    public HousingLand? GetLandByAddress(int addressId) =>
        _lands.Values.FirstOrDefault(l => l.Addresses.Any(a => a.Id == addressId));

    public Building? GetBuilding(int buildingId) => _buildingDefs.GetValueOrDefault(buildingId);

    private void LoadBuildingDefs(string path, ILogger log)
    {
        if (!File.Exists(path)) { log.LogWarning("HousingData: house_buildings.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });

        int buildingId = 0;
        BuildingType? type = null;
        HouseType? size = null;
        string? partsMatch = null;
        int? roof = null, outwall = null, frame = null, garden = null, fence = null;
        int door = 0, inwall = 0, infloor = 0;
        bool inBuilding = false;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.LocalName)
                {
                    case "building":
                        buildingId = int.TryParse(reader.GetAttribute("id"), out int bid) ? bid : 0;
                        type = ParseBuildingType(reader.GetAttribute("type"));
                        size = ParseHouseType(reader.GetAttribute("size"));
                        partsMatch = reader.GetAttribute("parts_match");
                        roof = outwall = frame = garden = fence = null;
                        door = inwall = infloor = 0;
                        inBuilding = true;
                        continue;
                    case "roof" when inBuilding: roof = reader.ReadElementContentAsInt(); continue;
                    case "outwall" when inBuilding: outwall = reader.ReadElementContentAsInt(); continue;
                    case "frame" when inBuilding: frame = reader.ReadElementContentAsInt(); continue;
                    case "door" when inBuilding: door = reader.ReadElementContentAsInt(); continue;
                    case "garden" when inBuilding: garden = reader.ReadElementContentAsInt(); continue;
                    case "fence" when inBuilding: fence = reader.ReadElementContentAsInt(); continue;
                    case "inwall" when inBuilding: inwall = reader.ReadElementContentAsInt(); continue;
                    case "infloor" when inBuilding: infloor = reader.ReadElementContentAsInt(); continue;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "building" && inBuilding)
            {
                if (buildingId != 0)
                {
                    var parts = roof is not null || outwall is not null || frame is not null || door != 0
                        || garden is not null || fence is not null || inwall != 0 || infloor != 0
                        ? new HouseParts(roof, outwall, frame, door, garden, fence, inwall, infloor)
                        : null;
                    _buildingDefs[buildingId] = new Building(buildingId, IsDefault: false, type, size, partsMatch, parts);
                }
                inBuilding = false;
            }
        }
    }

    private void LoadLands(string path, ILogger log)
    {
        if (!File.Exists(path)) { log.LogWarning("HousingData: houses.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });

        int landId = 0;
        int teleportNpc = 0, managerNpc = 0, homeSign = 0, waitingSign = 0, saleSign = 0, nosaleSign = 0;
        List<HouseAddress>? addresses = null;
        List<Building>? buildings = null;
        Sale? sale = null;
        long fee = 0;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.LocalName)
                {
                    case "land":
                        landId = int.TryParse(reader.GetAttribute("id"), out int lid) ? lid : 0;
                        teleportNpc = int.TryParse(reader.GetAttribute("teleport_npc"), out int tp) ? tp : 0;
                        managerNpc = int.TryParse(reader.GetAttribute("manager_npc"), out int mn) ? mn : 0;
                        homeSign = int.TryParse(reader.GetAttribute("sign_home"), out int hs) ? hs : 0;
                        waitingSign = int.TryParse(reader.GetAttribute("sign_waiting"), out int ws) ? ws : 0;
                        saleSign = int.TryParse(reader.GetAttribute("sign_sale"), out int ss) ? ss : 0;
                        nosaleSign = int.TryParse(reader.GetAttribute("sign_nosale"), out int ns) ? ns : 0;
                        addresses = new List<HouseAddress>();
                        buildings = new List<Building>();
                        sale = null;
                        fee = 0;
                        continue;

                    case "address" when addresses is not null:
                        var address = new HouseAddress(
                            Id: int.TryParse(reader.GetAttribute("id"), out int aid) ? aid : 0,
                            MapId: int.TryParse(reader.GetAttribute("map"), out int map) ? map : 0,
                            TownId: int.TryParse(reader.GetAttribute("town"), out int town) ? town : 0,
                            X: ParseFloat(reader.GetAttribute("x")),
                            Y: ParseFloat(reader.GetAttribute("y")),
                            Z: ParseFloat(reader.GetAttribute("z")),
                            ExitMapId: int.TryParse(reader.GetAttribute("exit_map"), out int em) ? em : null,
                            ExitX: ParseNullableFloat(reader.GetAttribute("exit_x")),
                            ExitY: ParseNullableFloat(reader.GetAttribute("exit_y")),
                            ExitZ: ParseNullableFloat(reader.GetAttribute("exit_z")));
                        addresses.Add(address);
                        _addresses[address.Id] = address;
                        continue;

                    case "building" when buildings is not null:
                        int refId = int.TryParse(reader.GetAttribute("id"), out int refBid) ? refBid : 0;
                        bool isDefault = string.Equals(reader.GetAttribute("default"), "true", StringComparison.OrdinalIgnoreCase);
                        var def = _buildingDefs.GetValueOrDefault(refId);
                        buildings.Add(def is not null
                            ? def with { IsDefault = isDefault }
                            : new Building(refId, isDefault, null, null, null, null));
                        continue;

                    case "sale":
                        sale = new Sale(
                            MinLevel: int.TryParse(reader.GetAttribute("level"), out int lvl) ? lvl : 0,
                            GoldPrice: long.TryParse(reader.GetAttribute("gold_price"), out long gp) ? gp : 0,
                            PointPrice: int.TryParse(reader.GetAttribute("point_price"), out int pp) ? pp : 0);
                        continue;

                    case "fee":
                        fee = reader.ReadElementContentAsLong();
                        continue;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "land" && landId != 0)
            {
                _lands[landId] = new HousingLand(
                    landId, teleportNpc, managerNpc, homeSign, waitingSign, saleSign, nosaleSign,
                    addresses ?? new List<HouseAddress>(),
                    buildings ?? new List<Building>(),
                    sale ?? new Sale(0, 0, 0),
                    fee);
                landId = 0;
            }
        }
    }

    private static float ParseFloat(string? value) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;

    private static float? ParseNullableFloat(string? value) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : null;

    private static BuildingType? ParseBuildingType(string? value) => value switch
    {
        "PERSONAL_FIELD" => BuildingType.PERSONAL_FIELD,
        "PERSONAL_INS" => BuildingType.PERSONAL_INS,
        _ => null,
    };

    private static HouseType? ParseHouseType(string? value) => value switch
    {
        "ESTATE" => HouseType.ESTATE,
        "MANSION" => HouseType.MANSION,
        "HOUSE" => HouseType.HOUSE,
        "STUDIO" => HouseType.STUDIO,
        "PALACE" => HouseType.PALACE,
        _ => null,
    };
}
