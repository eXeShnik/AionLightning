// KamarSiegeWeaponAI2 — Java ai/instance/kamarBattlefield/KamarSiegeWeaponAI2.java. Usable siege
// weapon prop: casts a faction-specific buff skill on use, then respawns.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kamarsiegeweapon")]
public sealed class KamarSiegeWeaponAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        switch (getOwner().Template.NpcId)
        {
            // Kamar Tank Elyos / IDKamar light-dark siege weapon and Idgel-machine variants.
            case 701898:
            case 701909:
            case 701910:
            case 701911:
            case 701912:
            case 701925:
            case 701926:
            case 701927:
            case 701928:
            case 701929:
            case 701930:
                UseSkill(21403);
                break;
            case 701899: // Kamar Tank Asmodians.
                UseSkill(21404);
                break;
        }
        // note: Java cast these no-animation skills directly on the player (not the owner) via
        // SkillEngine, then called AI2Actions.scheduleRespawn/deleteOwner; player-targeted skill casting
        // and AI2Actions aren't exposed to scripts yet.
    }
}
