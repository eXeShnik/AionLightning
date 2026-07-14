using AionLightning.Game.Model.Templates.Npc;

namespace AionLightning.Game.Model.GameObjects;

/// <summary>
/// A deployed kisk (bindstone): Java <c>model.gameobjects.Kisk extends SummonedObject&lt;Player&gt;</c>.
/// This port's <see cref="Npc"/> is sealed (composition over Java's class-per-behavior hierarchy — see
/// <see cref="Siege.SiegeNpc"/> for the same pattern), so Kisk wraps the spawned <see cref="Npc"/>
/// instead of extending it. Tracks the placer's race/legion (for the bind eligibility check), the
/// bound-member roster, and the remaining resurrect count; lifetime is wall-clock from
/// <see cref="SpawnUnixSeconds"/> rather than a stored expiry so <see cref="RemainingLifetime"/> stays
/// accurate even if the server clock is queried much later than the despawn timer fires.
/// Constructed and registered by <see cref="AionLightning.Game.Services.KiskService.SpawnKiskAsync"/>.
/// </summary>
public sealed class Kisk
{
    /// <summary>Java Kisk.KISK_LIFETIME_IN_SEC — fixed 2-hour lifetime regardless of kisk tier.</summary>
    public const int LifetimeSeconds = 2 * 60 * 60;

    public Npc Npc { get; }

    /// <summary>ObjectId of the player who placed this kisk (Java Kisk.getCreatorId() via SummonedObject).</summary>
    public int OwnerId { get; }
    public Race OwnerRace { get; }

    /// <summary>Legion id of the placer at spawn time, or null when unaffiliated (Java Kisk.ownerLegion).</summary>
    public int? OwnerLegionId { get; }

    /// <summary>1=race, 2=legion, 3=solo, 4=group, 5=alliance (Java Kisk.getUseMask()).</summary>
    public int UseMask { get; }
    public int MaxMembers { get; }
    public int MaxResurrects { get; }
    public int RemainingResurrects { get; set; }

    /// <summary>Bound players' objectIds (Java Kisk.kiskMemberIds).</summary>
    public HashSet<int> MemberIds { get; } = new();

    private readonly long _spawnUnixSeconds;

    public Kisk(Npc npc, Player owner, KiskStatsTemplate? statsTemplate)
    {
        Npc = npc;
        OwnerId = owner.ObjectId;
        OwnerRace = owner.Race;
        OwnerLegionId = owner.Legion?.LegionId;

        var stats = statsTemplate ?? new KiskStatsTemplate();
        UseMask = stats.UseMask;
        MaxMembers = stats.MaxMembers;
        MaxResurrects = stats.MaxResurrects;
        RemainingResurrects = stats.MaxResurrects;

        _spawnUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>Java Kisk.getRemainingLifetime() — seconds left before the despawn timer fires, floored at 0.</summary>
    public int RemainingLifetime
    {
        get
        {
            long elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _spawnUnixSeconds;
            int remaining = (int)(LifetimeSeconds - elapsed);
            return remaining > 0 ? remaining : 0;
        }
    }

    /// <summary>Java Kisk.isActive() — still standing and has at least one resurrect left.</summary>
    public bool IsActive => !Npc.IsAlreadyDead && RemainingResurrects > 0;
}
