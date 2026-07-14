using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Tribe;

namespace AionLightning.Game.Services;

/// <summary>
/// Faithful port of Java <c>services.TribeRelationService</c> — the Creature-level layer on top of
/// <see cref="TribeRelationsData"/>'s raw XML relation lookups. Adds the "base tribe" special cases
/// hard-coded in the Java service (guards are always aggressive toward the opposing race regardless of
/// what tribe_relations.xml says, monsters are always hostile toward players, etc.) before falling back
/// to the data-driven relation.
///
/// Registered as a DI singleton (unlike the Java static-method class) so it can depend on
/// <see cref="IDataManager"/> through the constructor, per this project's DI conventions.
/// </summary>
public sealed class TribeRelationService
{
    private readonly TribeRelationsData _data;

    public TribeRelationService(IDataManager dataManager) => _data = dataManager.TribeRelations;

    /// <summary>Java <c>Creature.getBaseTribe()</c>, resolved centrally here since <see cref="Creature"/>
    /// subtypes (models) don't hold a reference to <see cref="IDataManager"/>.</summary>
    private TribeClass BaseTribeOf(Creature creature) => _data.GetBaseTribe(creature.Tribe);

    /// <summary>Java <c>TribeRelationService.isAggressive(Creature, Creature)</c>.</summary>
    public bool IsAggressive(Creature creature1, Creature creature2)
    {
        var base1 = BaseTribeOf(creature1);
        var base2 = BaseTribeOf(creature2);

        if (base1 == TribeClass.GuardDark)
        {
            if (base2 == TribeClass.Pc || base2 == TribeClass.Guard || base2 == TribeClass.General || base2 == TribeClass.GuardDragon)
                return true;
        }
        else if (base1 == TribeClass.Guard)
        {
            if (base2 == TribeClass.PcDark || base2 == TribeClass.GuardDark || base2 == TribeClass.GeneralDark || base2 == TribeClass.GuardDragon)
                return true;
        }
        else if (base1 == TribeClass.GuardDragon)
        {
            if (base2 == TribeClass.PcDark || base2 == TribeClass.Pc || base2 == TribeClass.Guard
                || base2 == TribeClass.GuardDark || base2 == TribeClass.GeneralDark || base2 == TribeClass.General)
                return true;
        }

        return _data.IsAggressiveRelation(creature1.Tribe, creature2.Tribe);
    }

    /// <summary>Java <c>TribeRelationService.isFriend(Creature, Creature)</c>.</summary>
    public bool IsFriend(Creature creature1, Creature creature2)
    {
        if (creature1.Tribe == creature2.Tribe) return true;

        var base1 = BaseTribeOf(creature1);
        var base2 = BaseTribeOf(creature2);

        if (base1 == TribeClass.UseAll || base1 == TribeClass.FieldObjectAll)
            return true;
        if (base1 == TribeClass.GeneralDark)
        {
            if (base2 == TribeClass.PcDark || base2 == TribeClass.GuardDark) return true;
        }
        else if (base1 == TribeClass.General)
        {
            if (base2 == TribeClass.Pc || base2 == TribeClass.Guard) return true;
        }
        else if (base1 == TribeClass.FieldObjectLight || base1 == TribeClass.FieldObjectDark)
        {
            // Faithfully preserves a Java quirk: the FIELD_OBJECT_LIGHT case has no break before
            // FIELD_OBJECT_DARK, so it falls through into that check too — a FIELD_OBJECT_LIGHT tribe
            // ends up friendly toward PC_DARK as well, not just PC. FIELD_OBJECT_DARK reached directly
            // (not via fallthrough) only ever checks PC_DARK.
            if (base1 == TribeClass.FieldObjectLight && base2 == TribeClass.Pc) return true;
            if (base2 == TribeClass.PcDark) return true;
        }

        return _data.IsFriendlyRelation(creature1.Tribe, creature2.Tribe);
    }

    /// <summary>Java <c>TribeRelationService.isSupport(Creature, Creature)</c>.</summary>
    public bool IsSupport(Creature creature1, Creature creature2)
    {
        var base1 = BaseTribeOf(creature1);
        var base2 = BaseTribeOf(creature2);

        if (base1 == TribeClass.GuardDark)
        {
            if (base2 == TribeClass.PcDark) return true;
        }
        else if (base1 == TribeClass.Guard)
        {
            if (base2 == TribeClass.Pc) return true;
        }

        return _data.IsSupportRelation(creature1.Tribe, creature2.Tribe);
    }

    /// <summary>Java <c>TribeRelationService.isNeutral(Creature, Creature)</c>.</summary>
    public bool IsNeutral(Creature creature1, Creature creature2)
        => _data.IsNeutralRelation(creature1.Tribe, creature2.Tribe);

    /// <summary>Java <c>TribeRelationService.isHostile(Creature, Creature)</c>. Java's
    /// <c>checkSiegeRelation</c> guard is omitted — abyss/siege NPC types (fortress general guards vs
    /// the opposing race) aren't modeled yet (see migration_plan.md siege/instance gaps).</summary>
    public bool IsHostile(Creature creature1, Creature creature2)
    {
        if (BaseTribeOf(creature1) == TribeClass.Monster)
        {
            var base2 = BaseTribeOf(creature2);
            if (base2 == TribeClass.PcDark || base2 == TribeClass.Pc) return true;
        }

        return _data.IsHostileRelation(creature1.Tribe, creature2.Tribe);
    }

    /// <summary>Java <c>TribeRelationService.isNone(Creature, Creature)</c>. Note the guard below calls
    /// the raw <see cref="TribeRelationsData"/> relations directly (matching Java, which calls
    /// <c>DataManager.TRIBE_RELATIONS_DATA</c> here rather than this class's own
    /// <see cref="IsAggressive"/>/<see cref="IsHostile"/>) — the base-tribe special cases those methods
    /// add do not apply to this check. Java's <c>checkSiegeRelation</c> guard is omitted, same as
    /// <see cref="IsHostile"/>.</summary>
    public bool IsNone(Creature creature1, Creature creature2)
    {
        if (_data.IsAggressiveRelation(creature1.Tribe, creature2.Tribe)
            || _data.IsHostileRelation(creature1.Tribe, creature2.Tribe)
            || _data.IsNeutralRelation(creature1.Tribe, creature2.Tribe))
        {
            return false;
        }

        var base1 = BaseTribeOf(creature1);
        if (base1 == TribeClass.GeneralDragon)
            return true;

        var base2 = BaseTribeOf(creature2);
        if (base1 == TribeClass.General || base1 == TribeClass.FieldObjectLight)
        {
            if (base2 == TribeClass.PcDark) return true;
        }
        else if (base1 == TribeClass.GeneralDark || base1 == TribeClass.FieldObjectDark)
        {
            if (base2 == TribeClass.Pc) return true;
        }

        return _data.IsNoneRelation(creature1.Tribe, creature2.Tribe);
    }

    /// <summary>Perf guard for aggro-scan loops (Java call sites check
    /// <c>DataManager.TRIBE_RELATIONS_DATA.hasAggressiveRelations</c>/<c>hasHostileRelations</c>
    /// directly): true if this creature's tribe carries any aggro or hostile relation at all, so tribes
    /// with neither (the common case for passive/neutral NPCs) can be skipped before any per-target
    /// comparison.</summary>
    public bool HasAggroRelations(Creature creature)
        => _data.HasAggressiveRelations(creature.Tribe) || _data.HasHostileRelations(creature.Tribe);

    /// <summary>Perf guard for support/callForHelp loops (Java <c>hasSupportRelations</c>).</summary>
    public bool HasSupportRelations(Creature creature)
        => _data.HasSupportRelations(creature.Tribe);
}
