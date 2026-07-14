// AhbanaTheWicketAI2 — Java ai/instance/beshmundirTemple/AhbanaTheWicketAI2.java. Beshmundir Temple
// boss: shouts on engage and starts a repeating "Weeping Curtain" skill cast once below 75% HP.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("ahbanathewicked")]
public sealed class AhbanaTheWicketAI2 : AggressiveNpcAI2
{
    private const int WeepingCurtainSkillId = 18892;

    private bool _isHome = true;
    private bool _weepingCurtainStarted;

    public override void OnSpawned()
    {
        base.OnSpawned();
        _weepingCurtainStarted = false;
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_isHome)
        {
            _isHome = false;
            SendMsg(1500045);
            // note: Java also called callForHelp(36) here; aggro spreading isn't exposed to scripts
            // (see AggressiveNpcAI2.CallForHelp).
        }
        CheckPercentage(Owner.HpPercentage);
    }

    public override void OnDied()
    {
        base.OnDied(); // cancels the weeping-curtain task
        SendMsg(1500047);
    }

    public override void OnBackHome()
    {
        base.OnBackHome();
        _isHome = true;
        CancelTasks();
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 75 && !_weepingCurtainStarted)
        {
            _weepingCurtainStarted = true;
            ScheduleTask(() => UseSkill(WeepingCurtainSkillId), 10000, 40000);
        }
    }
}
