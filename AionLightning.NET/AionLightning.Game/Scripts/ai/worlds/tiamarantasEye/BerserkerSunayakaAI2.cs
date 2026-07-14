// BerserkerSunayakaAI2 — Java ai/worlds/tiamarantasEye/BerserkerSunayakaAI2.java. Casts a rage skill
// on its first attack after returning home.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("berserker_sunayaka")]
public sealed class BerserkerSunayakaAI2 : AggressiveNpcAI2
{
    private bool _isHome = true;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            if (Owner.Template.NpcId == 219311)
                UseSkill(20651, 1); // ragetask
        }
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isHome = true;
        // note: Java also removed effects 20651/8763 from the owner here when npcId == 219311; effect
        // removal by id isn't exposed to scripts yet.
    }
}
