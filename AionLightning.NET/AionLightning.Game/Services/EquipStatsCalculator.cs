using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Services;

/// <summary>
/// Aggregates stat bonuses from equipped items including their socketed manastones.
/// Used by PlayerEnterWorldService and CM_EQUIP_ITEM so manastone bonuses are
/// always included when recomputing player stats.
/// </summary>
public static class EquipStatsCalculator
{
    public readonly record struct EquipStats(
        int BonusMaxHp,
        int BonusMaxMp,
        int PhysicalDefense,
        int MagicDefense,
        int PhysicalAttackBonus,
        int MagicResistBonus,
        int MagicAttackBonus);

    public static EquipStats Compute(IEnumerable<Item> equippedItems, IDataManager dm)
    {
        int hp = 0, mp = 0, pDef = 0, mDef = 0, pAtk = 0, mRes = 0, mAtk = 0;

        foreach (var item in equippedItems)
        {
            Accumulate(item.ItemId, dm, ref hp, ref mp, ref pDef, ref mDef, ref pAtk, ref mRes, ref mAtk);
            foreach (var stone in item.ManaStones)
                Accumulate(stone.ItemId, dm, ref hp, ref mp, ref pDef, ref mDef, ref pAtk, ref mRes, ref mAtk);
        }

        return new EquipStats(hp, mp, pDef, mDef, pAtk, mRes, mAtk);
    }

    private static void Accumulate(int itemId, IDataManager dm,
        ref int hp, ref int mp, ref int pDef, ref int mDef, ref int pAtk, ref int mRes, ref int mAtk)
    {
        var tpl = dm.Items.GetTemplate(itemId);
        if (tpl is null) return;
        hp   += tpl.MaxHpBonus;
        mp   += tpl.MaxMpBonus;
        pDef += tpl.PhysicalDefense;
        mDef += tpl.MagicDefense;
        pAtk += tpl.PhysicalAttackBonus;
        mRes += tpl.MagicResistBonus;
        mAtk += tpl.MagicAttackBonus;
    }
}
