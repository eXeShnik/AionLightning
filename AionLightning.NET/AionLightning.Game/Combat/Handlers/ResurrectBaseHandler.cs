using AionLightning.Commons.Events;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M379: Handles &lt;resurrectbase&gt; debuff (Chain of Suffering I-VII).
/// When the debuffed player's HP reaches 0, auto-revives them at their bind point
/// before the normal death path runs. Mirrors Java ResurrectBaseEffect DEATH observer:
///   startEffect → attach DEATH observer → on died: bindRevive(effected, skillId)
///                                                 → SM_EMOTION(RESURRECT) + SM_PLAYER_SPAWN
/// Because this fires synchronously within DeathEvent (which is published inside
/// ApplyDamageAndPublishAsync before the caller's death branch), restoring HP here
/// causes the calling code's "if (HP > 0) return" guard to skip the death path entirely.
/// </summary>
public sealed class ResurrectBaseHandler(
    IDataManager             dataManager,
    PlayerConnectionRegistry connRegistry,
    IPlayerDao               playerDao)
    : IEventHandler<DeathEvent>
{
    public async ValueTask HandleAsync(DeathEvent e, CancellationToken ct)
    {
        if (e.Victim is not Player victim) return;

        var effects = victim.GetActiveEffects();
        if (effects.Count == 0) return;

        AbnormalState? resurrectEffect = null;
        foreach (var ab in effects)
        {
            if (ab.ResurrectBaseSkillId != 0) { resurrectEffect = ab; break; }
        }
        if (resurrectEffect is null) return;

        // Consume the one-time debuff immediately so it never triggers twice
        victim.RemoveEffectBySkillId(resurrectEffect.SkillId);

        // Determine bind point or initial spawn destination
        Position destination;
        if (victim.BindPosition.HasValue)
            destination = victim.BindPosition.Value;
        else
        {
            var spawn = dataManager.PlayerInitial.GetSpawnLocation(victim.Race);
            destination = new Position(spawn.X, spawn.Y, spawn.Z, spawn.Heading, spawn.MapId);
        }

        // Apply soul sickness (mirrors Java bindRevive → revive(player, 25, 25, true, skillId))
        if (victim.SoulSicknessCount < 10)
        {
            victim.SoulSicknessCount++;
            _ = playerDao.UpdateSoulSicknessAsync(victim.ObjectId, victim.SoulSicknessCount, CancellationToken.None);
        }

        // Recompute MaxHp/MaxMp with updated soul sickness multiplier
        var statTpl = dataManager.PlayerStats.GetTemplate(victim.PlayerClass, victim.Level);
        float ssMult = victim.SoulSicknessMultiplier;
        victim.MaxHp = (int)(((statTpl?.MaxHp ?? 1000) + victim.BonusMaxHp + victim.PassiveBonusMaxHp + victim.TitleBonusMaxHp) * ssMult);
        victim.MaxMp = (int)(((statTpl?.MaxMp ?? 500)  + victim.BonusMaxMp + victim.PassiveBonusMaxMp + victim.TitleBonusMaxMp) * ssMult);

        if (victim.SoulSicknessCount > 0)
            victim.AddEffect(new AbnormalState
            {
                SkillId    = 8291,
                SkillLevel = victim.SoulSicknessCount,
                EffectorId = victim.ObjectId,
                Expiry     = DateTime.MaxValue
            });

        // Restore to 25% HP/MP — this is the critical step that prevents the death path
        victim.CurrentHp = Math.Max(1, victim.MaxHp * 25 / 100);
        victim.CurrentMp = Math.Max(1, victim.MaxMp * 25 / 100);

        var conn = connRegistry.Get(victim.ObjectId);
        if (conn is null) return;

        int oldWorldId = victim.Position.WorldId;
        var oldScope = victim.Position;
        bool crossZone = destination.WorldId != oldWorldId;

        if (crossZone)
        {
            var deletePkt = new SM_DELETE(victim.ObjectId);
            foreach (var c in connRegistry.GetAllExcept(victim.ObjectId))
                if (c.ActivePlayer is { } cp && cp.Position.SameScope(oldScope))
                    try { await c.SendAsync(deletePkt, ct); } catch { }
        }

        victim.Position = destination;
        try { await conn.SendAsync(new SM_TELEPORT_LOC(destination), ct); } catch { }

        var scope = victim.Position;

        if (crossZone)
        {
            _ = SchedulePostReviveSpawnAsync(victim, conn, ct);
        }
        else
        {
            var resurrectEmotion = new SM_EMOTION(victim, EmotionType.RESURRECT);
            try { await conn.SendAsync(resurrectEmotion, ct); } catch { }
            foreach (var c in connRegistry.GetAllExcept(victim.ObjectId))
                if (c.ActivePlayer is { } cp && cp.Position.SameScope(scope))
                    try { await c.SendAsync(resurrectEmotion, ct); } catch { }

            var standEmotion = new SM_EMOTION(victim, EmotionType.STAND);
            try { await conn.SendAsync(standEmotion, ct); } catch { }
            foreach (var c in connRegistry.GetAllExcept(victim.ObjectId))
                if (c.ActivePlayer is { } cp && cp.Position.SameScope(scope))
                    try { await c.SendAsync(standEmotion, ct); } catch { }

            var ssAbnormal = new SM_ABNORMAL_EFFECT(victim.ObjectId, isPlayer: true, victim.GetActiveEffects());
            try { await conn.SendAsync(ssAbnormal, ct); } catch { }
        }

        try { await conn.SendAsync(new SM_STATS_INFO(victim, statTpl, dataManager.ExpTable), ct); } catch { }
        try { await conn.SendAsync(SM_SYSTEM_MESSAGE.Revived(), ct); } catch { }
    }

    private static async Task SchedulePostReviveSpawnAsync(Player victim, GsClientConnection conn, CancellationToken ct)
    {
        try
        {
            await Task.Delay(2200, ct);
            await conn.SendAsync(new SM_CHANNEL_INFO(), ct);
            await conn.SendAsync(new SM_PLAYER_SPAWN(victim), ct);
        }
        catch (OperationCanceledException) { }
    }
}
