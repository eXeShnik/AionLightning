using AionLightning.Commons.Events;
using AionLightning.Game.Combat;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Controllers;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Zone;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

/// <summary>
/// Java <c>controllers.movement.PlayerMoveController.updateFalling</c>/<c>stopFalling</c> +
/// <c>utils.stats.StatFunctions.calculateFallDamage</c> ported. Tracks each player's cumulative
/// descent (<see cref="Player.FallDistance"/>/<see cref="Player.LastFallZ"/>, updated per CM_MOVE
/// tick while the FALL movement bit is set) and applies HP loss — or an instant kill — once the
/// player lands or free-falls past the mid-air threshold.
/// </summary>
public sealed class FallDamageService
{
    private readonly FallDamageOptions _options;
    private readonly IEventBus _eventBus;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ZoneService _zoneService;
    private readonly FlyController _flyController;

    public FallDamageService(IOptions<FallDamageOptions> options, IEventBus eventBus,
        PlayerConnectionRegistry connRegistry, ZoneService zoneService, FlyController flyController)
    {
        _options       = options.Value;
        _eventBus      = eventBus;
        _connRegistry  = connRegistry;
        _zoneService   = zoneService;
        _flyController = flyController;
    }

    /// <summary>
    /// Java <c>updateFalling</c> — called every CM_MOVE tick while the FALL bit is set. Accumulates
    /// descent since the last tick and kills the player mid-air once the cumulative distance passes
    /// <see cref="FallDamageOptions.MaximumDistanceMidair"/> (Java has no flying/gliding guard here —
    /// the client only sets the FALL bit while genuinely free-falling).
    /// </summary>
    public async ValueTask UpdateFallingAsync(Player player, float newZ, GsClientConnection conn, CancellationToken ct)
    {
        if (player.LastFallZ != 0)
        {
            player.FallDistance += player.LastFallZ - newZ;
            if (_options.Enable && player.FallDistance >= _options.MaximumDistanceMidair)
                await CalculateFallDamageAsync(player, player.FallDistance, stopped: false, conn, ct);
        }
        player.LastFallZ = newZ;
    }

    /// <summary>
    /// Java <c>stopFalling</c> — called once the FALL bit clears (landed, or otherwise stopped
    /// descending). Applies fall damage for the accumulated distance, then resets tracking.
    /// Skipped while flying/gliding (Java's <c>!owner.isFlying()</c> guard) or inside a water zone
    /// (not an explicit Java check, but a genuine landing-in-water should never hurt — added defensively).
    /// </summary>
    public async ValueTask StopFallingAsync(Player player, float newZ, GsClientConnection conn, CancellationToken ct)
    {
        if (player.LastFallZ != 0)
        {
            bool isFlying = player.State.HasFlag(CreatureState.Flying) || player.State.HasFlag(CreatureState.Gliding);
            if (_options.Enable && !isFlying && !_zoneService.IsInsideZoneType(player, ZoneType.Water))
                await CalculateFallDamageAsync(player, player.FallDistance, stopped: true, conn, ct);

            player.FallDistance = 0;
            player.LastFallZ    = 0;
        }
    }

    /// <summary>
    /// Java <c>StatFunctions.calculateFallDamage</c>. <paramref name="stopped"/> mirrors Java's
    /// <c>stoped</c> parameter — false means this is the mid-air check (always lethal once called),
    /// true means this is a genuine landing (lethal only past <see cref="FallDamageOptions.MaximumDistance"/>).
    /// </summary>
    private async ValueTask CalculateFallDamageAsync(Player player, float distance, bool stopped, GsClientConnection conn, CancellationToken ct)
    {
        if (player.IsAlreadyDead) return;

        if (distance >= _options.MaximumDistance || !stopped)
        {
            // note: Java also calls player.getController().onStopMove() here to halt movement state;
            // no equivalent generic "stop move" hook exists in this port yet.
            await _flyController.StopGlidingAsync(player, removeWings: false, conn, ct);
            await ApplyFallDamageAsync(player, player.MaxHp + 1, conn, ct);
            return;
        }

        if (distance >= _options.MinimumDistance)
        {
            int damage = (int)(distance * (player.MaxHp * _options.DamagePercentage / 100f));
            await ApplyFallDamageAsync(player, damage, conn, ct);
            try { await conn.SendAsync(new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.FallDamage, 0, -damage), ct); } catch { }
        }
    }

    // note: routes through the shared damage pipeline (ApplyDamageAndPublishAsync), so unlike Java's
    // direct reduceHp() call, Shield/MpShield/Protect/Sanctuary handlers may partially or fully absorb
    // fall damage here. Reusing the canonical HP/death path (DeathEvent -> quest/instance/pvp hooks)
    // was preferred over a bespoke raw HP write that would bypass those subscribers.
    private async ValueTask ApplyFallDamageAsync(Player player, int damage, GsClientConnection conn, CancellationToken ct)
    {
        bool wasAlive = player.CurrentHp > 0;
        await player.ApplyDamageAndPublishAsync(player, damage, DamageKind.Fall, null, _eventBus, ct);

        if (wasAlive && player.CurrentHp == 0)
            await HandleFallDeathAsync(player, conn, ct);
    }

    /// <summary>
    /// Self-inflicted death visual broadcast — mirrors the dead-player block duplicated inline at
    /// every other damage call site (CM_ATTACK/CM_CASTSPELL/NpcAiService) since this codebase has no
    /// centralized DeathEvent-driven SM_DIE broadcast yet. Java's generic
    /// <c>PlayerController.onDie</c> additionally releases summons, ends duels, runs instance/map-region
    /// death hooks, rewards, and quest onDie — those are out of scope here since <c>ApplyDamageAndPublishAsync</c>
    /// already publishes <see cref="DeathEvent"/> (quest/instance/pvp subscribers still fire).
    /// </summary>
    private async ValueTask HandleFallDeathAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        player.State |= CreatureState.Dead;
        player.ClearAllEffects();

        int worldId = player.Position.WorldId;
        var emotionPkt = new SM_EMOTION(player, EmotionType.DIE);
        var abnormalPkt = new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true);
        try { await conn.SendAsync(emotionPkt, ct); } catch { }
        try { await conn.SendAsync(abnormalPkt, ct); } catch { }
        foreach (var other in _connRegistry.GetAllExcept(player.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
            {
                try { await other.SendAsync(emotionPkt, ct); } catch { }
                try { await other.SendAsync(abnormalPkt, ct); } catch { }
            }

        try { await conn.SendAsync(new SM_DIE(), ct); } catch { }
    }
}
