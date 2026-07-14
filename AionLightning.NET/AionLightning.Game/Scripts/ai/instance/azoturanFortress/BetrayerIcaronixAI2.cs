// BetrayerIcaronixAI2 — Java ai/instance/azoturanFortress/BetrayerIcaronixAI2.java. Betrayer
// Icaronix: at 50% HP, schedules its "true form" (Icaronix the Betrayer, 214599) to spawn 5s later in
// its place.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("betrayericaronix")]
public sealed class BetrayerIcaronixAI2 : AggressiveNpcAI2
{
    private bool _eventStarted;

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        if (_eventStarted || Owner.HpPercentage > 50) return;
        _eventStarted = true;
        var pos = Owner.Position;
        ScheduleTask(() => Spawn(214599, pos.X, pos.Y, pos.Z), 5000);
        // note: Java also called AI2Actions.deleteOwner(this) immediately here to remove itself; no C#
        // equivalent delete-owner action exists yet.
    }
}
