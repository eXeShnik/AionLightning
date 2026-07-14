using AionLightning.Commons.Events;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Services;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// Java services.siegeservice.SiegeBossDoAddDamageListener — Java registered this as an
/// <c>AggroList.AddDamageValueCallback</c> directly on the boss NPC's aggro list. This port has no ai2/AOP
/// callback framework (see CLAUDE.md), so instead this subscribes to the shared DamageDealtEvent bus and
/// feeds <see cref="Services.Siege.Siege.AddBossDamage"/> whenever the hit target is a tracked siege boss.
/// </summary>
public sealed class SiegeBossDamageHandler(SiegeService siegeService) : IEventHandler<DamageDealtEvent>
{
    public ValueTask HandleAsync(DamageDealtEvent e, CancellationToken ct)
    {
        if (e.Target is not Npc npc) return ValueTask.CompletedTask;
        if (siegeService.GetSiegeNpc(npc) is not { IsBoss: true } siegeNpc) return ValueTask.CompletedTask;
        if (siegeService.GetSiege(siegeNpc.SiegeId) is not { } siege) return ValueTask.CompletedTask;
        if (siege.Boss?.Npc.ObjectId != npc.ObjectId) return ValueTask.CompletedTask;

        siege.AddBossDamage(e.Attacker, e.DamageAmount);
        return ValueTask.CompletedTask;
    }
}
