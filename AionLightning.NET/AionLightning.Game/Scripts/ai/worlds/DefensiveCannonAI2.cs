// DefensiveCannonAI2 — Java ai/worlds/DefensiveCannonAI2.java. Siege turret: morphs the user into a
// cannon-mode skill once, per npc-id lookup table.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("defensive_cannon")]
public sealed class DefensiveCannonAI2 : ActionItemNpcAI2
{
    private bool _canUse = true;

    protected override void HandleUseItemFinish(Player player)
    {
        lock (this)
        {
            if (!_canUse) return;
            _canUse = false;
        }
        int morphSkill = GetMorphSkill();
        if (morphSkill != 0)
            UseSkill(morphSkill >> 8, morphSkill & 0xFF);
        // note: Java also called AI2Actions.deleteOwner(this) here; no owner-delete hook is exposed to
        // scripts yet.
    }

    private int GetMorphSkill() => Owner.Template.NpcId switch
    {
        831338 => 0x4F8C3C, // elyos defensive cannon (Invade Vortex 3.5 Theobomos/Bruthonin)
        831339 => 0x4F8D3C, // asmodian defensive cannon (Invade Vortex 3.5 Theobomos/Bruthonin)
        273313 or 272841 or 272848 => 0x538900, // Empty Aetheric Cannon / mounted elyos cannons (Danaria Sieges 4.0)
        273315 or 272854 or 272861 => 0x538A00, // Empty Etched Cannon / mounted asmodian cannons (Danaria Sieges 4.0)
        251723 => 0x525300, // Lightbringer (Tank Station Abyss 4.5)
        251724 => 0x525400, // Shadecaster (Tank Station Abyss 4.5)
        _ => 0,
    };

    // note: Java also overrode pollInstance(SHOULD_REWARD) to refuse XP/loot reward; no poll-question
    // hook exists on NpcAi2 yet.
}
