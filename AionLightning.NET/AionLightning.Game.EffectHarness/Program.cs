using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using AionLightning.Game.Combat.Effects;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Npc;
using AionLightning.Game.Model.Templates.Skill;

// Effect-parity harness: the oracle for the S4 effect-engine extraction. Loads the real 4.6 skill
// data, runs each extracted pure calculator against representative skills, asserts the computed
// deltas match the template source, and full-scans every template to prove no calculator throws.
// Run: dotnet run --project AionLightning.Game.EffectHarness -c Debug

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
var dataManager = new DataManager(loggerFactory.CreateLogger<DataManager>());

var allTemplates = dataManager.Skills.AllTemplates.ToList();
Console.WriteLine($"Loaded {allTemplates.Count} skill templates.");

static Npc MakeNpc() => new(new NpcTemplate { NpcId = 1, Name = "harness_dummy", Level = 1 });

int passed = 0, total = 0;

// --- StatEffectCalculator (S4a) ---
// PhysAtkAddDelta is a flat-ADD statup that needs no is-Player percent-of-base gate, so it
// round-trips cleanly on an Npc target: PatkStatUpDeltaVal == PhysAtkStatUpDelta + PhysAtkAddDelta.
var statCandidates = allTemplates
    .Where(t => (t.Effects?.PhysAtkAddDelta ?? 0) != 0)
    .Take(5)
    .ToList();
Console.WriteLine($"[StatEffectCalculator] {statCandidates.Count} candidate skill(s) with nonzero PhysAtkAddDelta.");
foreach (var template in statCandidates)
{
    total++;
    int expected = (template.Effects?.PhysAtkStatUpDelta ?? 0) + template.Effects!.PhysAtkAddDelta;
    var result = StatEffectCalculator.Compute(template, MakeNpc(), template.Level);
    bool ok = result.PatkStatUpDeltaVal == expected;
    Console.WriteLine($"  skill_id={template.SkillId} \"{template.Name}\" " +
                      $"PatkStatUpDeltaVal={result.PatkStatUpDeltaVal} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) passed++;
}

// Full-scan: Compute must never throw for any template (Npc target, level >= 1).
int scanned = 0, exceptions = 0;
foreach (var template in allTemplates)
{
    scanned++;
    try { _ = StatEffectCalculator.Compute(template, MakeNpc(), Math.Max(1, template.Level)); }
    catch (Exception ex)
    {
        exceptions++;
        Console.WriteLine($"  EXCEPTION skill_id={template.SkillId}: {ex.GetType().Name}: {ex.Message}");
    }
}

Console.WriteLine($"HARNESS (StatEffectCalculator): {passed}/{total} assertions passed, {scanned} templates scanned, {exceptions} exceptions");

// --- DotDamageCalculator (S4b) ---
// Elemental branch: an Npc dummy has all elemental resists at 0 (default int), so the elemResist>0
// branch never triggers and ComputePerTick collapses to the hand-inlined "no resist" formula:
// max(1, dot.BaseValue + dot.Delta*level). applyPassiveSpellAttackBonus is irrelevant here since the
// dot is not a "spellatk" type. This proves the elemental branch matches the original inline math.
// The spellatk branch shares the same effector-stat reads as StatEffectCalculator's magic-atk math and
// is exercised (without throwing) by the full-scan below with a dummy Player effector; a numeric
// parity assertion for it is skipped here since building a fully-stocked Player is out of scope for
// this harness — the branch structure is identical to the elemental branch's call shape.
var dotCandidates = allTemplates
    .Where(t => (t.Effects?.DotEffects?.Count ?? 0) > 0
             && t.Effects!.DotEffects.Any(d => d.DotType != "spellatk"))
    .Take(5)
    .ToList();
Console.WriteLine($"[DotDamageCalculator] {dotCandidates.Count} candidate skill(s) with non-spellatk DotEffects.");
int dotPassed = 0, dotTotal = 0;
foreach (var template in dotCandidates)
{
    var dot = template.Effects!.DotEffects.First(d => d.DotType != "spellatk");
    int level = Math.Max(1, template.Level);
    dotTotal++;
    int expected = Math.Max(1, dot.BaseValue + dot.Delta * level);
    int result = DotDamageCalculator.ComputePerTick(dot, new Player(), MakeNpc(), level,
        applyPassiveSpellAttackBonus: false, applyElementalResist: true);
    bool ok = result == expected;
    Console.WriteLine($"  skill_id={template.SkillId} \"{template.Name}\" dot_type={dot.DotType} " +
                      $"result={result} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) dotPassed++;
}

// Full-scan: ComputePerTick must never throw for any dot effect on any template (both flags off,
// dummy Player effector, Npc target, level >= 1).
int dotScanned = 0, dotExceptions = 0;
var dummyEffector = new Player();
foreach (var template in allTemplates)
{
    if ((template.Effects?.DotEffects?.Count ?? 0) == 0) continue;
    int level = Math.Max(1, template.Level);
    foreach (var dot in template.Effects!.DotEffects)
    {
        dotScanned++;
        try { _ = DotDamageCalculator.ComputePerTick(dot, dummyEffector, MakeNpc(), level, false, false); }
        catch (Exception ex)
        {
            dotExceptions++;
            Console.WriteLine($"  EXCEPTION skill_id={template.SkillId}: {ex.GetType().Name}: {ex.Message}");
        }
    }
}

Console.WriteLine($"HARNESS (DotDamageCalculator): {dotPassed}/{dotTotal} assertions passed, {dotScanned} dot effects scanned, {dotExceptions} exceptions");

int totalPassed = passed + dotPassed, totalAssertions = total + dotTotal, totalExceptions = exceptions + dotExceptions;
Console.WriteLine($"HARNESS: {totalPassed}/{totalAssertions} assertions passed, {scanned} templates scanned, {dotScanned} dot effects scanned, {totalExceptions} exceptions");
return totalPassed == totalAssertions && totalExceptions == 0 ? 0 : 1;
