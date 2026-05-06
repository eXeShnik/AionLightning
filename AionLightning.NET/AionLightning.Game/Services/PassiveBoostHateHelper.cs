using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;

namespace AionLightning.Game.Services;

/// <summary>M333: computes the total BOOST_HATE % bonus from PASSIVE skills (e.g. Gladiator Aggravation).</summary>
public static class PassiveBoostHateHelper
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
            total += skillTpl.Effects.BoostHateStatPct;
        }
        return total;
    }
}
