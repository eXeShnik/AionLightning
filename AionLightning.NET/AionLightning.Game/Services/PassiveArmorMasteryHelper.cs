using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;

namespace AionLightning.Game.Services;

/// <summary>M288: computes the total armor mastery pdef% bonus based on player's passive skills and current equipment.</summary>
public static class PassiveArmorMasteryHelper
{
    public static int ComputePct(Player player, IDataManager dm)
    {
        var armorTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in player.Inventory.All)
        {
            if (!item.IsEquipped) continue;
            var tpl = dm.Items.GetTemplate(item.ItemId);
            if (tpl?.IsArmor == true && !string.IsNullOrEmpty(tpl.ArmorTypeName))
                armorTypes.Add(tpl.ArmorTypeName);
        }
        if (armorTypes.Count == 0) return 0;

        int total = 0;
        foreach (var skillEntry in player.Skills.AllSkills)
        {
            var skillTpl = dm.Skills.GetTemplate(skillEntry.SkillId);
            if (skillTpl?.Effects is null
                || !string.Equals(skillTpl.Activation, "PASSIVE", StringComparison.OrdinalIgnoreCase))
                continue;
            foreach (var info in skillTpl.Effects.ArmorMasteryEffects)
            {
                if (armorTypes.Contains(info.ArmorType))
                    total += info.PdefPct;
            }
        }
        return total;
    }
}
