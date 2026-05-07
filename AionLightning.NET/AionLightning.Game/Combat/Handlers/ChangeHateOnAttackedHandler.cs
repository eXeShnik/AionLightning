using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M376: Handles &lt;changehateonattacked&gt; — when a player carrying this buff is attacked by an NPC,
/// the NPC's hate towards them is changed by value1+value2 (negative = hate reduction).
/// Java analog: ChangeHateOnAttackedEffect (ActionObserver ATTACKED).
/// </summary>
public sealed class ChangeHateOnAttackedHandler(
    IDataManager dataManager)
    : IEventHandler<DamageDealtEvent>
{
    public ValueTask HandleAsync(DamageDealtEvent e, CancellationToken ct)
    {
        if (e.Attacker is not Npc npc) return ValueTask.CompletedTask;
        if (npc.IsAlreadyDead) return ValueTask.CompletedTask;

        var target = e.Target;
        var effects = target.GetActiveEffects();
        if (effects.Count == 0) return ValueTask.CompletedTask;

        foreach (var ab in effects)
        {
            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            if (tpl?.Effects?.HasChangeHateOnAtk != true) continue;

            var fxList = tpl.Effects.ChangeHateOnAtkEffects;
            if (fxList is not { Count: > 0 }) continue;

            foreach (var fx in fxList)
            {
                int delta = fx.Value1 + fx.Value2; // negative = reduce NPC hate
                npc.AddHate(target.ObjectId, delta);
            }
            break;
        }
        return ValueTask.CompletedTask;
    }
}
