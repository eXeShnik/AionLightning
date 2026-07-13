using System.Xml;
using AionLightning.Game.Model.Templates.Pet;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Loads the toy-pet feed minigame data: pets/pet_feed.xml (flavours + reward groups, Java
/// <c>ItemGroupsData</c>-adjacent <c>PetFlavour</c>/<c>PetRewards</c>/<c>PetFeedResult</c> templates) and
/// the feed-related item-group membership sets from items/item_groups.xml (Java
/// <c>ItemGroupsData.isFood</c>). Only the feed_*/poppy_snack/aether-biscuit/shugo/stinking_junk groups
/// are loaded here — the ~30 other bonus/craft/manastone/ore groups in that file are unrelated to pet
/// feed and are intentionally not ported.
/// </summary>
public sealed class PetFeedData
{
    // Java ItemGroupsData: element name -> FoodType this port cares about.
    private static readonly Dictionary<string, FoodType> ElementToFoodType = new()
    {
        ["feed_fluid"] = FoodType.FLUIDS,
        ["feed_armor"] = FoodType.ARMOR,
        ["feed_thorn"] = FoodType.THORNS,
        ["feed_bone"] = FoodType.BONES,
        ["feed_balaur_material"] = FoodType.BALAUR_SCALES,
        ["feed_soul"] = FoodType.SOULS,
        ["feed_exclude"] = FoodType.EXCLUDES,
        ["stinking_junk"] = FoodType.STINKY,
        ["feed_healthy_all"] = FoodType.HEALTHY_FOOD_ALL,
        ["feed_healthy_spicy"] = FoodType.HEALTHY_FOOD_SPICY,
        ["feed_powder_biscuit"] = FoodType.AETHER_POWDER_BISCUIT,
        ["feed_crystal_biscuit"] = FoodType.AETHER_CRYSTAL_BISCUIT,
        ["feed_gem_biscuit"] = FoodType.AETHER_GEM_BISCUIT,
        ["poppy_snack"] = FoodType.POPPY_SNACK,
        ["tasty_poppy_snack"] = FoodType.POPPY_SNACK_TASTY,
        ["nutritious_poppy_snack"] = FoodType.POPPY_SNACK_NUTRITIOUS,
        ["feed_shugo_event_coin"] = FoodType.SHUGO_EVENT_COIN,
    };

    // Java ItemGroupsData.isFood — MISCELLANEOUS is a synthetic aggregate of these groups.
    private static readonly FoodType[] MiscellaneousGroups =
        [FoodType.ARMOR, FoodType.BALAUR_SCALES, FoodType.BONES, FoodType.FLUIDS, FoodType.SOULS, FoodType.THORNS];

    private readonly Dictionary<int, PetFlavour> _flavours = new();
    private readonly Dictionary<FoodType, HashSet<int>> _foodGroups = new();

    public int FlavourCount => _flavours.Count;
    public IReadOnlyCollection<PetFlavour> Flavours => _flavours.Values;

    public void Load(string dataRoot, ILogger log)
    {
        LoadFlavours(Path.Combine(dataRoot, "pets", "pet_feed.xml"), log);
        LoadFoodGroups(Path.Combine(dataRoot, "items", "item_groups.xml"), log);
    }

    public PetFlavour? GetFlavourById(int id) => _flavours.GetValueOrDefault(id);

    /// <summary>Mirrors Java PetFlavour.getFoodType(itemId) — resolves the reward group (if any) this
    /// item satisfies for the flavour, based on item-group membership.</summary>
    public PetRewards? ResolveFoodGroup(PetFlavour flavour, int itemId)
    {
        foreach (var rewards in flavour.Food)
            if (IsFood(itemId, rewards.Type))
                return rewards;
        return null;
    }

    /// <summary>Java ItemGroupsData.isFood(itemId, foodType).</summary>
    public bool IsFood(int itemId, FoodType foodType)
    {
        if (_foodGroups.TryGetValue(FoodType.EXCLUDES, out var excludes) && excludes.Contains(itemId)) return false;
        if (_foodGroups.TryGetValue(FoodType.STINKY, out var stinky) && stinky.Contains(itemId)) return false;

        if (foodType != FoodType.MISCELLANEOUS)
            return _foodGroups.TryGetValue(foodType, out var set) && set.Contains(itemId);

        foreach (var group in MiscellaneousGroups)
            if (_foodGroups.TryGetValue(group, out var set) && set.Contains(itemId))
                return true;
        return false;
    }

    private void LoadFlavours(string path, ILogger log)
    {
        if (!File.Exists(path)) { log.LogWarning("PetFeedData: pet_feed.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });

        int flavourId = 0, fullCount = 1, lovedLimit = 0, cooldown = 0;
        List<PetRewards>? food = null;
        FoodType currentGroupType = FoodType.MISCELLANEOUS;
        bool currentLoved = false;
        List<PetFeedResult>? currentResults = null;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.LocalName)
                {
                    case "flavour":
                        flavourId = int.TryParse(reader.GetAttribute("id"), out int fid) ? fid : 0;
                        fullCount = int.TryParse(reader.GetAttribute("full_count"), out int fc) ? fc : 1;
                        lovedLimit = int.TryParse(reader.GetAttribute("loved_limit"), out int ll) ? ll : 0;
                        cooldown = int.TryParse(reader.GetAttribute("cd"), out int cd) ? cd : 0;
                        food = new List<PetRewards>();
                        break;
                    case "food":
                        currentGroupType = Enum.TryParse(reader.GetAttribute("group"), out FoodType ft) ? ft : FoodType.MISCELLANEOUS;
                        currentLoved = bool.TryParse(reader.GetAttribute("loved"), out bool lv) && lv;
                        currentResults = new List<PetFeedResult>();
                        break;
                    case "result":
                        if (currentResults is not null && int.TryParse(reader.GetAttribute("item"), out int itemId))
                            currentResults.Add(new PetFeedResult(itemId, reader.GetAttribute("name")));
                        break;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (reader.LocalName == "food" && food is not null && currentResults is not null)
                {
                    food.Add(new PetRewards(currentGroupType, currentLoved, currentResults));
                    currentResults = null;
                }
                else if (reader.LocalName == "flavour" && flavourId != 0)
                {
                    _flavours[flavourId] = new PetFlavour(flavourId, fullCount, lovedLimit, cooldown, food ?? new List<PetRewards>());
                    flavourId = 0;
                }
            }
        }

        log.LogInformation("PetFeedData: loaded {Count} pet feed flavours", _flavours.Count);
    }

    private void LoadFoodGroups(string path, ILogger log)
    {
        if (!File.Exists(path)) { log.LogWarning("PetFeedData: item_groups.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });

        FoodType? current = null;
        int itemCount = 0;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (ElementToFoodType.TryGetValue(reader.LocalName, out var foodType))
                {
                    current = foodType;
                    _foodGroups.TryAdd(foodType, new HashSet<int>());
                    continue;
                }
                if (reader.LocalName == "item" && current is { } ft && int.TryParse(reader.GetAttribute("id"), out int id))
                {
                    _foodGroups[ft].Add(id);
                    itemCount++;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement && ElementToFoodType.ContainsKey(reader.LocalName))
            {
                current = null;
            }
        }

        log.LogInformation("PetFeedData: loaded {Count} pet-food item entries across {Groups} feed groups", itemCount, _foodGroups.Count);
    }
}
