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

Console.WriteLine($"HARNESS: {passed}/{total} assertions passed, {scanned} templates scanned, {exceptions} exceptions");
return passed == total && exceptions == 0 ? 0 : 1;
