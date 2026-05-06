using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;

namespace AionLightning.Game.Services;

/// <summary>M344: computes the total shieldmastery BLOCK% bonus from the player's passive skills.</summary>
public static class PassiveShieldMasteryHelper
{
    public static int ComputePct(Player player, IDataManager dm)
    {
        int total = 0;
        foreach (var skillEntry in player.Skills.AllSkills)
        {
            var skillTpl = dm.Skills.GetTemplate(skillEntry.SkillId);
            if (skillTpl?.Effects is null
                || !string.Equals(skillTpl.Activation, "PASSIVE", StringComparison.OrdinalIgnoreCase))
                continue;
            int pct = skillTpl.Effects.ShieldMasteryBlockPct;
            if (pct > 0)
                total += pct;
        }
        return total;
    }
}
