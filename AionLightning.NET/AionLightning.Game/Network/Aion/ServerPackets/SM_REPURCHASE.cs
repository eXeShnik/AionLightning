using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the player's repurchase (buy-back) list for an NPC shop. Opcode 0xA7.</summary>
public sealed class SM_REPURCHASE : AionServerPacket
{
    private readonly int _npcObjectId;
    private readonly IReadOnlyList<RepurchaseService.RepurchaseEntry> _entries;

    public SM_REPURCHASE(int npcObjectId, IReadOnlyList<RepurchaseService.RepurchaseEntry> entries)
        : base(0xA7)
    {
        _npcObjectId = npcObjectId;
        _entries     = entries;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_npcObjectId);
        w.WriteD(1);
        w.WriteH((short)_entries.Count);

        foreach (var e in _entries)
        {
            var proxy = new Item { UniqueId = e.UniqueId, ItemId = e.ItemId, Count = e.Count, Slot = -1 };
            SM_INVENTORY_INFO.WriteItemInfo(ref w, proxy);
            w.WriteQ(e.RepurchasePrice);
        }
    }
}
