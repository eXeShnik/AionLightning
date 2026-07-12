using AionLightning.Game.Model;

namespace AionLightning.Game.Combat.Effects;

/// <summary>
/// S4e: shared home for two small combat-math helpers that were duplicated as private statics in
/// both CM_ATTACK and CM_CASTSPELL — <c>NpcLevelDiffMod</c> (identical body in both files) and
/// <c>IsBehindTarget</c> (only in CM_CASTSPELL). Bodies copied verbatim; both packet handlers now
/// delegate to this class instead of holding their own copy. No behavior change.
/// </summary>
public static class CombatMath
{
    // Java StatFunctions.getNpcLevelDiffMod: multiplier for dodge and damage when NPC > player level
    public static float NpcLevelDiffMod(int levelDiff) => levelDiff switch
    {
        3 => 0.1f, 4 => 0.2f, 5 => 0.3f, 6 => 0.4f,
        7 => 0.5f, 8 => 0.6f, 9 => 0.7f,
        _ => levelDiff > 9 ? 0.8f : 0f
    };

    // M304: Java PositionUtil.isBehindTarget — caster is behind target when angle(caster→target) ≈ target's facing (±90°)
    // Heading: 0-119 units × 3 = 0-357°. atan2 in degrees, normalized 0-360. MAX_ANGLE_DIFF = 90°.
    public static bool IsBehindTarget(Position caster, Position target)
    {
        float angleFromCaster = (float)(Math.Atan2(target.Y - caster.Y, target.X - caster.X) * (180.0 / Math.PI));
        if (angleFromCaster < 0f) angleFromCaster += 360f;
        float targetFacing = target.Heading * 3f;
        float diff = angleFromCaster - targetFacing;
        if (diff <= -270f) diff += 360f;
        if (diff >= 270f) diff -= 360f;
        return Math.Abs(diff) <= 90f;
    }
}
