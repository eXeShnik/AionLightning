using System.Xml;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class ShopData
{
    // npcId -> ordered goodslist IDs (for SM_TRADELIST tab order)
    private readonly Dictionary<int, List<int>> _npcGoodsListIds = new();
    // npcId -> flat set of all item template IDs the NPC sells
    private readonly Dictionary<int, HashSet<int>> _npcItems = new();
    // goodslist id -> item template IDs
    private readonly Dictionary<int, List<int>> _goodsLists = new();

    public void Load(string dataRoot, ILogger log)
    {
        LoadGoodsLists(dataRoot, log);
        LoadNpcTradeLists(dataRoot, log);
    }

    private void LoadGoodsLists(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "goodslists", "goodslists.xml");
        if (!File.Exists(path)) { log.LogWarning("ShopData: goodslists not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        int? currentListId = null;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "list")
            {
                if (int.TryParse(reader.GetAttribute("id"), out int listId))
                {
                    currentListId = listId;
                    _goodsLists[listId] = new List<int>();
                }
            }
            else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "item" && currentListId.HasValue)
            {
                if (int.TryParse(reader.GetAttribute("id"), out int itemId))
                    _goodsLists[currentListId.Value].Add(itemId);
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "list")
            {
                currentListId = null;
            }
        }

        log.LogInformation("ShopData: loaded {Count} goods lists", _goodsLists.Count);
    }

    private void LoadNpcTradeLists(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "npc_trade_list.xml");
        if (!File.Exists(path)) { log.LogWarning("ShopData: npc_trade_list.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        int currentNpcId = 0;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "tradelist_template")
            {
                if (int.TryParse(reader.GetAttribute("npc_id"), out int npcId))
                {
                    currentNpcId = npcId;
                    if (!_npcGoodsListIds.ContainsKey(npcId))
                        _npcGoodsListIds[npcId] = new List<int>();
                    if (!_npcItems.ContainsKey(npcId))
                        _npcItems[npcId] = new HashSet<int>();
                }
            }
            else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "tradelist" && currentNpcId != 0)
            {
                if (int.TryParse(reader.GetAttribute("id"), out int listId))
                {
                    if (!_npcGoodsListIds[currentNpcId].Contains(listId))
                        _npcGoodsListIds[currentNpcId].Add(listId);

                    if (_goodsLists.TryGetValue(listId, out var items))
                        foreach (var itemId in items)
                            _npcItems[currentNpcId].Add(itemId);
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "tradelist_template")
            {
                currentNpcId = 0;
            }
        }

        log.LogInformation("ShopData: loaded trade lists for {Count} NPCs", _npcGoodsListIds.Count);
    }

    public bool HasShop(int npcId) => _npcGoodsListIds.ContainsKey(npcId);

    public IReadOnlyList<int>? GetGoodsListIds(int npcId)
        => _npcGoodsListIds.TryGetValue(npcId, out var list) ? list : null;

    public bool NpcSellsItem(int npcId, int itemId)
        => _npcItems.TryGetValue(npcId, out var set) && set.Contains(itemId);

    public IReadOnlySet<int>? GetItemsForNpc(int npcId)
        => _npcItems.TryGetValue(npcId, out var set) ? set : null;
}
