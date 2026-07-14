using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts a player's visual equipment state to all players in range. Opcode 0x24.</summary>
public sealed class SM_UPDATE_PLAYER_APPEARANCE : AionServerPacket
{
    private readonly int _playerObjectId;
    private readonly IReadOnlyList<Item> _equipped;

    public SM_UPDATE_PLAYER_APPEARANCE(int playerObjectId, IEnumerable<Item> allItems) : base(0x24)
    {
        _playerObjectId = playerObjectId;
        _equipped = allItems.Where(i => i.IsEquipped).ToList();
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjectId);

        // Equipment slot mask: OR of all equipped slot values. Truncated to 32 bits per iteration —
        // mirrors Java SM_UPDATE_PLAYER_APPEARANCE's `mask = (int)(mask | item.getEquipmentSlot())`;
        // stigma slot bits (30-52) are intentionally lost here since stigmas carry no visible appearance.
        int mask = _equipped.Aggregate(0, (acc, item) => (int)((uint)acc | (uint)item.Slot));
        w.WriteD(mask);

        foreach (var item in _equipped)
        {
            w.WriteD(item.SkinItemId != 0 ? item.SkinItemId : item.ItemId); // skin template ID
            w.WriteD(item.GodStoneItemId);             // godstone itemId (0 = none)
            w.WriteD(item.DyeColor);                   // dye color — dye item's templateId (0 = undyed)
            w.WriteD(item.EnchantLevel >= 15 ? 1 : 0); // enchant glow
        }
    }
}
