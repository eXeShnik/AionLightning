using AionLightning.Game.Combat.Effects;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.CuringZone;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java <c>services.CuringZoneService</c> — spawns every curing-point template from
/// <see cref="CuringObjectsData"/> and, on a 1-second tick, applies the curing skill (8751, "Light of
/// Repose" — a self-targeted HP/MP HoT) to every player within a curing object's range that doesn't
/// already carry the effect. Java gated re-application on <c>!hasAbnormalEffect(8751)</c>; the same
/// check here is <c>Player.GetActiveEffects().Any(e => e.SkillId == CuringSkillId)</c>.
///
/// Java called <c>SkillEngine.getSkill(player, 8751, 1, player).useNoAnimationSkill()</c> — there is no
/// ported SkillEngine (no generic "cast an arbitrary skill outside packet context" service exists yet),
/// so the HoT application below mirrors CM_CASTSPELL's own HotEffects branch directly: register the
/// AbnormalState + one EffectTickScheduler tick per &lt;heal&gt;/&lt;mpheal&gt; element, exactly like a
/// real cast would. If skill 8751 ever ships with no HotEffects data, the tick + range detection still
/// run — only the heal application is skipped (see the note below).
/// </summary>
public sealed class CuringZoneService : BackgroundService
{
    private const int CuringSkillId = 8751;
    private const int CuringSkillLevel = 1;
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private readonly List<CuringObject> _curingObjects = new();
    private readonly IDataManager _dataManager;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly EffectTickScheduler _effectTickScheduler;
    private readonly ILogger<CuringZoneService> _log;

    public CuringZoneService(IDataManager dataManager, GameWorld world, PlayerConnectionRegistry connRegistry,
        EffectTickScheduler effectTickScheduler, ILogger<CuringZoneService> log)
    {
        _dataManager         = dataManager;
        _world               = world;
        _connRegistry        = connRegistry;
        _effectTickScheduler = effectTickScheduler;
        _log                 = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        foreach (var template in _dataManager.CuringObjects.CuringObjects)
            _curingObjects.Add(new CuringObject(template));
        _log.LogInformation("CuringZoneService: spawned {Count} curing zones", _curingObjects.Count);

        using var timer = new PeriodicTimer(TickInterval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            foreach (var obj in _curingObjects)
            {
                foreach (var player in _world.GetPlayersInScope(obj.Position))
                {
                    if (player.IsAlreadyDead) continue;
                    if (player.Position.DistanceTo(obj.Position) > obj.Range) continue;
                    if (player.GetActiveEffects().Any(e => e.SkillId == CuringSkillId)) continue;

                    try { await ApplyCuringSkillAsync(player, ct); }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "CuringZoneService: failed applying curing skill to {Player}", player.Name);
                    }
                }
            }
        }
    }

    private async ValueTask ApplyCuringSkillAsync(Player player, CancellationToken ct)
    {
        var template = _dataManager.Skills.GetTemplate(CuringSkillId);
        var hotEffects = template?.Effects?.HotEffects;
        if (hotEffects is not { Count: > 0 })
        {
            // note: skill 8751 has no <heal>/<mpheal> HotEffects loaded — nothing to apply here, but the
            // 1s tick + range detection above stay fully functional regardless (Java-fidelity gap only).
            return;
        }

        int durationMs = hotEffects.Max(h => h.Duration2Ms);
        var effect = new AbnormalState
        {
            SkillId    = CuringSkillId,
            SkillLevel = CuringSkillLevel,
            EffectorId = player.ObjectId,
            Expiry     = DateTime.UtcNow.AddMilliseconds(durationMs),
        };
        player.AddEffect(effect);

        int worldId = player.Position.WorldId;
        var abnormalPkt = new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true, player.GetActiveEffects());
        foreach (var c in _connRegistry.GetAll())
            if (c.ActivePlayer?.Position.WorldId == worldId)
                try { await c.SendAsync(abnormalPkt, ct); } catch { }

        foreach (var hot in hotEffects)
        {
            int healPerTick = HealAmountCalculator.ComputeHotTickBase(hot, CuringSkillLevel);
            var registry = _connRegistry;
            _effectTickScheduler.Register(player, player, effect, CuringSkillId, hot.CheckTimeMs, effect.Expiry,
                onTick: async tickCtx =>
                {
                    int actual = hot.HealType switch
                    {
                        "hp" => Math.Min(healPerTick, tickCtx.Effected.MaxHp - tickCtx.Effected.CurrentHp),
                        _    => Math.Min(healPerTick, tickCtx.Effected.MaxMp - tickCtx.Effected.CurrentMp),
                    };
                    if (actual <= 0) return;

                    int tickWorldId = tickCtx.Effected.Position.WorldId;
                    if (hot.HealType == "hp")
                    {
                        tickCtx.Effected.CurrentHp += actual;
                        var pkt = new SM_ATTACK_STATUS(tickCtx.Effected, SM_ATTACK_STATUS.AttackType.NaturalHp, tickCtx.SkillId, actual, SM_ATTACK_STATUS.LogId.Heal);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == tickWorldId)
                                try { await c.SendAsync(pkt); } catch { }
                    }
                    else
                    {
                        tickCtx.Effected.CurrentMp += actual;
                        var pkt = new SM_ATTACK_STATUS(tickCtx.Effected, SM_ATTACK_STATUS.AttackType.NaturalMp, tickCtx.SkillId, actual, SM_ATTACK_STATUS.LogId.MpHeal);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == tickWorldId)
                                try { await c.SendAsync(pkt); } catch { }
                    }
                });
        }
    }
}
