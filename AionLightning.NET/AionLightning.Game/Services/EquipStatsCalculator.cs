using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Templates.Item;

namespace AionLightning.Game.Services;

/// <summary>
/// Aggregates stat bonuses from equipped items, their socketed manastones, and
/// enchant-level bonuses. Used by PlayerEnterWorldService, CM_EQUIP_ITEM, and
/// CM_MANASTONE so all three sources are always included when recomputing player stats.
/// Enchant formulas mirror Java StatEnchantFunction (StatEnchantFunction.java).
/// Hit/miss and crit formulas use stats provided by this calculator (M168).
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
        int MagicAttackBonus,
        int Evasion,
        int PhysicalAccuracy,
        int PhysicalCritical,
        int PhysicalCriticalResist,
        int MagicalAccuracy,
        int MagicalCritical,
        int MagicalCriticalResist,
        int AttackSpeedBonus,
        int Concentration,
        int MagicBoost,
        int MagicSuppression,
        int HealBoost,
        int Parry,
        int Block,
        int StrikeFortitude,
        int SpellFortitude,
        int MovementSpeedBonus,
        int FlySpeedBonus);

    public static EquipStats Compute(IEnumerable<Item> equippedItems, IDataManager dm)
    {
        int hp = 0, mp = 0, pDef = 0, mDef = 0, pAtk = 0, mRes = 0, mAtk = 0;
        int evade = 0, pAcc = 0, pCrit = 0, pCritRes = 0;
        int mAcc = 0, mCrit = 0, mCritRes = 0, atkSpd = 0;
        int conc = 0, mBoost = 0, mSuppress = 0, healBoost = 0, parry = 0, block = 0, sF = 0, spF = 0;
        int speed = 0, flySpd = 0;

        foreach (var item in equippedItems)
        {
            var tpl = dm.Items.GetTemplate(item.ItemId);
            if (tpl is not null)
            {
                AccumulateTemplate(tpl, ref hp, ref mp, ref pDef, ref mDef, ref pAtk, ref mRes, ref mAtk,
                    ref evade, ref pAcc, ref pCrit, ref pCritRes, ref mAcc, ref mCrit, ref mCritRes,
                    ref atkSpd, ref conc, ref mBoost, ref mSuppress, ref healBoost, ref parry, ref block, ref sF, ref spF, ref speed, ref flySpd);
                if (item.EnchantLevel > 0)
                    AccumulateEnchant(item.EnchantLevel, (int)item.Slot, tpl,
                        ref hp, ref pDef, ref mDef, ref pAtk, ref mAtk, ref pCritRes);
            }
            foreach (var stone in item.ManaStones)
                Accumulate(stone.ItemId, dm, ref hp, ref mp, ref pDef, ref mDef, ref pAtk, ref mRes, ref mAtk,
                    ref evade, ref pAcc, ref pCrit, ref pCritRes, ref mAcc, ref mCrit, ref mCritRes,
                    ref atkSpd, ref conc, ref mBoost, ref mSuppress, ref healBoost, ref parry, ref block, ref sF, ref spF, ref speed, ref flySpd);
        }

        return new EquipStats(hp, mp, pDef, mDef, pAtk, mRes, mAtk, evade, pAcc, pCrit, pCritRes, mAcc, mCrit, mCritRes,
            atkSpd, conc, mBoost, mSuppress, healBoost, parry, block, sF, spF, speed, flySpd);
    }

    private static void AccumulateTemplate(ItemTemplate tpl,
        ref int hp, ref int mp, ref int pDef, ref int mDef, ref int pAtk, ref int mRes, ref int mAtk,
        ref int evade, ref int pAcc, ref int pCrit, ref int pCritRes,
        ref int mAcc, ref int mCrit, ref int mCritRes,
        ref int atkSpd, ref int conc, ref int mBoost, ref int mSuppress, ref int healBoost,
        ref int parry, ref int block, ref int sF, ref int spF, ref int speed, ref int flySpd)
    {
        hp        += tpl.MaxHpBonus;
        mp        += tpl.MaxMpBonus;
        pDef      += tpl.PhysicalDefense;
        mDef      += tpl.MagicDefense;
        pAtk      += tpl.PhysicalAttackBonus;
        mRes      += tpl.MagicResistBonus;
        mAtk      += tpl.MagicAttackBonus;
        evade     += tpl.EvasionBonus;
        pAcc      += tpl.PhysicalAccuracyBonus;
        pCrit     += tpl.PhysicalCriticalBonus;
        pCritRes  += tpl.PhysicalCriticalResistBonus;
        mAcc      += tpl.MagicalAccuracyBonus;
        mCrit     += tpl.MagicalCriticalBonus;
        mCritRes  += tpl.MagicalCriticalResistBonus;
        atkSpd    += tpl.AttackSpeedBonusPct;
        conc      += tpl.ConcentrationBonus;
        mBoost    += tpl.MagicBoostBonus;
        mSuppress += tpl.MagicSuppressionBonus;
        healBoost += tpl.HealBoostBonus;
        parry     += tpl.ParryBonus;
        block     += tpl.BlockBonus;
        sF        += tpl.StrikeFortitudeBonus;
        spF       += tpl.SpellFortitudeBonus;
        speed     += tpl.SpeedBonusPct;
        flySpd    += tpl.FlySpeedBonusPct;
    }

    private static void Accumulate(int itemId, IDataManager dm,
        ref int hp, ref int mp, ref int pDef, ref int mDef, ref int pAtk, ref int mRes, ref int mAtk,
        ref int evade, ref int pAcc, ref int pCrit, ref int pCritRes,
        ref int mAcc, ref int mCrit, ref int mCritRes,
        ref int atkSpd, ref int conc, ref int mBoost, ref int mSuppress, ref int healBoost,
        ref int parry, ref int block, ref int sF, ref int spF, ref int speed, ref int flySpd)
    {
        var tpl = dm.Items.GetTemplate(itemId);
        if (tpl is null) return;
        AccumulateTemplate(tpl, ref hp, ref mp, ref pDef, ref mDef, ref pAtk, ref mRes, ref mAtk,
            ref evade, ref pAcc, ref pCrit, ref pCritRes, ref mAcc, ref mCrit, ref mCritRes,
            ref atkSpd, ref conc, ref mBoost, ref mSuppress, ref healBoost, ref parry, ref block, ref sF, ref spF, ref speed, ref flySpd);
    }

    // Java StatEnchantFunction.getEnchantAdditionModifier — off-hand slots skip weapon enchant bonus.
    // Slot bitmasks from Java ItemSlot: SUB_HAND=2, SUB_OFF_HAND=262144.
    // Armor slot groups: GLOVES=16, BOOTS=32, SHOULDER=2048 (small), PANTS=4096 (medium), TORSO=8 (large).
    // PHYSICAL_CRITICAL_RESIST and MAGICAL_DEFEND enchant bonuses added per Java (all armor types/slots).
    private static void AccumulateEnchant(int enchantLvl, int slot, ItemTemplate tpl,
        ref int hp, ref int pDef, ref int mDef, ref int pAtk, ref int mAtk, ref int pCritRes)
    {
        if (tpl.IsWeapon)
        {
            if (slot is 2 or 262144) return; // off-hand gets no enchant bonus

            switch (tpl.WeaponTypeName)
            {
                case "DAGGER_1H":
                case "SWORD_1H":
                    pAtk += 2 * enchantLvl;
                    break;
                case "POLEARM_2H":
                case "SWORD_2H":
                case "BOW":
                    pAtk += 4 * enchantLvl;
                    break;
                case "MACE_1H":
                case "STAFF_2H":
                    pAtk += 3 * enchantLvl;
                    break;
                case "BOOK_2H":
                case "ORB_2H":
                    mAtk += 3 * enchantLvl;
                    break;
                case "HARP_2H":
                case "CANNON_2H":
                case "KEYBLADE_2H":
                    mAtk += 4 * enchantLvl;
                    break;
                case "GUN_1H":
                    mAtk += 2 * enchantLvl;
                    break;
            }
        }
        else if (tpl.IsArmor)
        {
            switch (tpl.ArmorTypeName)
            {
                case "ROBE":
                    switch (slot)
                    {
                        case 16: case 32: case 2048:
                            pDef     += enchantLvl;     hp += 10 * enchantLvl;
                            mDef     += 2 * enchantLvl; pCritRes += 2 * enchantLvl; break;
                        case 4096:
                            pDef     += 2 * enchantLvl; hp += 12 * enchantLvl;
                            mDef     += 2 * enchantLvl; pCritRes += 3 * enchantLvl; break;
                        case 8:
                            pDef     += 3 * enchantLvl; hp += 14 * enchantLvl;
                            mDef     += 3 * enchantLvl; pCritRes += 4 * enchantLvl; break;
                    }
                    break;
                case "LEATHER":
                    switch (slot)
                    {
                        case 16: case 32: case 2048:
                            pDef     += 2 * enchantLvl; hp += 8  * enchantLvl;
                            mDef     += 2 * enchantLvl; pCritRes += 2 * enchantLvl; break;
                        case 4096:
                            pDef     += 3 * enchantLvl; hp += 10 * enchantLvl;
                            mDef     += 2 * enchantLvl; pCritRes += 3 * enchantLvl; break;
                        case 8:
                            pDef     += 4 * enchantLvl; hp += 12 * enchantLvl;
                            mDef     += 3 * enchantLvl; pCritRes += 4 * enchantLvl; break;
                    }
                    break;
                case "CHAIN":
                    switch (slot)
                    {
                        case 16: case 32: case 2048:
                            pDef     += 3 * enchantLvl; hp += 6  * enchantLvl;
                            mDef     += 2 * enchantLvl; pCritRes += 2 * enchantLvl; break;
                        case 4096:
                            pDef     += 4 * enchantLvl; hp += 8  * enchantLvl;
                            mDef     += 2 * enchantLvl; pCritRes += 3 * enchantLvl; break;
                        case 8:
                            pDef     += 5 * enchantLvl; hp += 10 * enchantLvl;
                            mDef     += 3 * enchantLvl; pCritRes += 4 * enchantLvl; break;
                    }
                    break;
                case "PLATE":
                    switch (slot)
                    {
                        case 16: case 32: case 2048:
                            pDef     += 4 * enchantLvl; hp += 4 * enchantLvl;
                            mDef     += 2 * enchantLvl; pCritRes += 2 * enchantLvl; break;
                        case 4096:
                            pDef     += 5 * enchantLvl; hp += 6 * enchantLvl;
                            mDef     += 2 * enchantLvl; pCritRes += 3 * enchantLvl; break;
                        case 8:
                            pDef     += 6 * enchantLvl; hp += 8 * enchantLvl;
                            mDef     += 3 * enchantLvl; pCritRes += 4 * enchantLvl; break;
                    }
                    break;
            }
        }
    }
}
