using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;

namespace AionLightning.Game.Services;

/// <summary>M291: computes PHYSICAL_ATTACK% and MAGICAL_ATTACK% bonuses from weapon mastery PASSIVE skills.</summary>
public static class PassiveWeaponMasteryHelper
{
    public static (int PhysAttPct, int MagAttPct) Compute(Player player, IDataManager dm)
    {
        var weaponTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in player.Inventory.All)
        {
            if (!item.IsEquipped) continue;
            var tpl = dm.Items.GetTemplate(item.ItemId);
            if (tpl?.IsWeapon == true && !string.IsNullOrEmpty(tpl.WeaponTypeName))
                weaponTypes.Add(tpl.WeaponTypeName);
        }
        if (weaponTypes.Count == 0) return (0, 0);

        int physPct = 0, magPct = 0;
        foreach (var skillEntry in player.Skills.AllSkills)
        {
            var skillTpl = dm.Skills.GetTemplate(skillEntry.SkillId);
            if (skillTpl?.Effects is null
                || !string.Equals(skillTpl.Activation, "PASSIVE", StringComparison.OrdinalIgnoreCase))
                continue;
            foreach (var info in skillTpl.Effects.WpnMasteryEffects)
            {
                if (!weaponTypes.Contains(info.WeaponType)) continue;
                if (string.Equals(info.Stat, "PHYSICAL_ATTACK", StringComparison.OrdinalIgnoreCase))
                    physPct += info.Pct;
                else if (string.Equals(info.Stat, "MAGICAL_ATTACK", StringComparison.OrdinalIgnoreCase))
                    magPct += info.Pct;
            }
        }
        return (physPct, magPct);
    }
}
