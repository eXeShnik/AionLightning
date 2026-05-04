using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M285: Handles &lt;provoker&gt; — when buffed NPC is attacked, attacker gets a massive hate
/// boost that makes the NPC target them. Java analog: ProvokerEffect (ActionObserver ATTACK).
/// </summary>
public sealed class ProvokerHandler(
    IDataManager dataManager)
    : IEventHandler<DamageDealtEvent>
{
    public ValueTask HandleAsync(DamageDealtEvent e, CancellationToken ct)
    {
        if (e.Target is not Npc provokedNpc) return ValueTask.CompletedTask;
        if (provokedNpc.IsAlreadyDead) return ValueTask.CompletedTask;

        var effects = provokedNpc.GetActiveEffects();
        if (effects.Count == 0) return ValueTask.CompletedTask;

        foreach (var ab in effects)
        {
            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            if (tpl?.Effects?.HasProvoker != true) continue;
            // M279 hate quantum: provoker adds enough hate that the attacker becomes top target
            provokedNpc.AddHate(e.Attacker.ObjectId, 5000);
            break;
        }
        return ValueTask.CompletedTask;
    }
}
