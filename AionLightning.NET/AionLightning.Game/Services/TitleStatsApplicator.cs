using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Player;

namespace AionLightning.Game.Services;

/// <summary>
/// Adds title stat bonuses to a player's bonus fields ON TOP of equipment bonuses.
/// Call this after EquipStatsCalculator.Compute and before computing derived stats
/// (CurrentAttackSpeed, MovementSpeed). MAXHP/MAXMP are intentionally excluded here
/// because they are tracked separately in TitleBonusMaxHp/TitleBonusMaxMp.
/// Java: PlayerGameStats adds all active title modifiers alongside equipment modifiers.
/// </summary>
public static class TitleStatsApplicator
{
    public static void Apply(Player player, PlayerTitleTemplate titleTpl)
    {
        // <add> stats — flat bonuses that stack with equipment
        player.BonusPhysicalAtk            += titleTpl.GetAddStat("PHYSICAL_ATTACK");
        player.PhysicalDefense             += titleTpl.GetAddStat("PHYSICAL_DEFENSE");
        player.MagicDefense                += titleTpl.GetAddStat("MAGICAL_DEFEND");
        player.BonusMagicResist            += titleTpl.GetAddStat("MAGICAL_RESIST");
        player.BonusMagicAtk               += titleTpl.GetAddStat("MAGICAL_ATTACK");
        player.BonusEvasion                += titleTpl.GetAddStat("EVASION");
        player.BonusPhysicalAccuracy       += titleTpl.GetAddStat("PHYSICAL_ACCURACY");
        player.BonusPhysicalCritical       += titleTpl.GetAddStat("PHYSICAL_CRITICAL");
        player.BonusPhysicalCriticalResist += titleTpl.GetAddStat("PHYSICAL_CRITICAL_RESIST");
        player.BonusMagicalAccuracy        += titleTpl.GetAddStat("MAGICAL_ACCURACY");
        player.BonusMagicalCritical        += titleTpl.GetAddStat("MAGICAL_CRITICAL");
        player.BonusMagicalCriticalResist  += titleTpl.GetAddStat("MAGICAL_CRITICAL_RESIST");
        player.BonusParry                  += titleTpl.GetAddStat("PARRY");
        player.BonusBlock                  += titleTpl.GetAddStat("BLOCK");
        player.BonusConcentration          += titleTpl.GetAddStat("CONCENTRATION");
        player.BonusMagicBoost             += titleTpl.GetAddStat("BOOST_MAGICAL_SKILL");
        player.BonusMagicSuppression       += titleTpl.GetAddStat("MAGIC_SKILL_BOOST_RESIST");
        player.BonusHealBoost              += titleTpl.GetAddStat("HEAL_BOOST");
        player.BonusStrikeFortitude        += titleTpl.GetAddStat("PHYSICAL_CRITICAL_DAMAGE_REDUCE");
        player.BonusSpellFortitude         += titleTpl.GetAddStat("MAGICAL_CRITICAL_DAMAGE_REDUCE");

        // <rate> stats — percentage bonuses that stack with equipment rate bonuses
        player.BonusMovementSpeedPct += titleTpl.GetRateStat("SPEED");
        player.BonusAttackSpeedPct   += titleTpl.GetRateStat("ATTACK_SPEED");
        player.BonusFlySpeedPct      += titleTpl.GetRateStat("FLY_SPEED");
        // BOOST_CASTING_TIME from titles adds to weapon cast time bonus
        player.WeaponCastTimeBonus   += titleTpl.GetRateStat("BOOST_CASTING_TIME");
    }
}
