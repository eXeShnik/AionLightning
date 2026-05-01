using AionLightning.Commons.Network;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Templates.Decomposable;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the selectable reward dialog for a box item. Opcode 0x11C.</summary>
public sealed class SM_SELECT_ITEM_LIST : AionServerPacket
{
    private readonly int              _uniqueItemId;
    private readonly List<SelectItem> _items;
    private readonly IDataManager     _dataManager;

    public SM_SELECT_ITEM_LIST(int uniqueItemId, SelectItems selectItems, IDataManager dataManager)
        : base(0x11C)
    {
        _uniqueItemId = uniqueItemId;
        _items        = selectItems.Items;
        _dataManager  = dataManager;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_uniqueItemId);
        w.WriteD(0);
        w.WriteC((byte)_items.Count);
        for (int i = 0; i < _items.Count; i++)
        {
            var si       = _items[i];
            var template = _dataManager.Items.GetTemplate(si.Id);
            w.WriteC((byte)i);
            w.WriteD(si.Id);
            w.WriteD(si.Count);
            w.WriteC(template is not null && template.OptionSlotBonus > 0 ? (byte)255 : (byte)0);
            w.WriteC(template is not null && template.MaxEnchantBonus > 0 ? (byte)255 : (byte)0);
            if (template is not null && (template.IsArmor || template.IsWeapon))
                w.WriteH(-1);
            else
                w.WriteH(0);
            if (template is not null && (template.IsCloth || template.OptionSlotBonus > 0 || template.MaxEnchantBonus > 0))
                w.WriteC(1);
            else
                w.WriteC(0);
        }
    }
}
