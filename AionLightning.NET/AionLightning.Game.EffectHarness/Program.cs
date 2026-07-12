using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AionLightning.Game.Combat.Effects;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Npc;
using AionLightning.Game.Model.Templates.Skill;
using AionLightning.Game.Services;

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

// --- DebuffEffectCalculator (S4d) ---
// PdefAddDelta is a flat-ADD statdown that needs no is-Player percent-of-target-base gate, so it
// round-trips cleanly on an Npc target: PdefDelta == PdefAddDelta (no percent debuff fires without a Player).
var debuffCandidates = allTemplates
    .Where(t => (t.Effects?.PdefAddDelta ?? 0) != 0)
    .Take(5)
    .ToList();
Console.WriteLine($"[DebuffEffectCalculator] {debuffCandidates.Count} candidate skill(s) with nonzero PdefAddDelta.");
int debuffPassed = 0, debuffTotal = 0;
foreach (var template in debuffCandidates)
{
    debuffTotal++;
    int expected = template.Effects!.PdefAddDelta;
    var result = DebuffEffectCalculator.Compute(template, MakeNpc(), template.Level);
    bool ok = result.PdefDelta == expected;
    Console.WriteLine($"  skill_id={template.SkillId} \"{template.Name}\" " +
                      $"PdefDelta={result.PdefDelta} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) debuffPassed++;
}

// Full-scan: Compute must never throw for any template (Npc target, level >= 1).
int debuffScanned = 0, debuffExceptions = 0;
foreach (var template in allTemplates)
{
    debuffScanned++;
    try { _ = DebuffEffectCalculator.Compute(template, MakeNpc(), Math.Max(1, template.Level)); }
    catch (Exception ex)
    {
        debuffExceptions++;
        Console.WriteLine($"  EXCEPTION skill_id={template.SkillId}: {ex.GetType().Name}: {ex.Message}");
    }
}

Console.WriteLine($"HARNESS (DebuffEffectCalculator): {debuffPassed}/{debuffTotal} assertions passed, {debuffScanned} templates scanned, {debuffExceptions} exceptions");

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

// --- HealAmountCalculator / DrainCalculator (S4c) ---
// Identity check: a non-percent hp heal on an Npc target (HealReceivedPct == 0, default int) with
// boostMult == 1.0f collapses ComputeInstant to the hand-inlined "flat value + delta*level" formula —
// no boost multiplier, no HealReceivedPct adjustment, no percent-of-max-stat branch.
var healCandidates = allTemplates
    .Where(t => (t.Effects?.HealEffects?.Count ?? 0) > 0
             && t.Effects!.HealEffects.Any(h => !h.IsPercent && h.HealType == "hp"))
    .Take(5)
    .ToList();
Console.WriteLine($"[HealAmountCalculator] {healCandidates.Count} candidate skill(s) with non-percent hp HealEffects.");
int healPassed = 0, healTotal = 0;
foreach (var template in healCandidates)
{
    var he = template.Effects!.HealEffects.First(h => !h.IsPercent && h.HealType == "hp");
    int level = Math.Max(1, template.Level);
    healTotal++;
    int expected = he.BaseValue + he.Delta * level;
    int result = HealAmountCalculator.ComputeInstant(he, MakeNpc(), level, 1.0f);
    bool ok = result == expected;
    Console.WriteLine($"  skill_id={template.SkillId} \"{template.Name}\" " +
                      $"result={result} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) healPassed++;
}

// ComputeHotTickBase: both useLevelMinusOne flag values against the hand-inlined formulas.
var hotCandidates = allTemplates
    .Where(t => (t.Effects?.HotEffects?.Count ?? 0) > 0)
    .Take(5)
    .ToList();
Console.WriteLine($"[HealAmountCalculator] {hotCandidates.Count} candidate skill(s) with HotEffects.");
foreach (var template in hotCandidates)
{
    var hot = template.Effects!.HotEffects[0];
    int level = Math.Max(1, template.Level);

    healTotal++;
    int expectedA2 = Math.Max(1, hot.BaseValue + hot.Delta * level);
    int resultA2 = HealAmountCalculator.ComputeHotTickBase(hot, level, useLevelMinusOne: false);
    bool okA2 = resultA2 == expectedA2;
    Console.WriteLine($"  skill_id={template.SkillId} \"{template.Name}\" (A2, level) " +
                      $"result={resultA2} (expected {expectedA2}) {(okA2 ? "PASS" : "FAIL")}");
    if (okA2) healPassed++;

    healTotal++;
    int expectedA5 = Math.Max(1, hot.BaseValue + hot.Delta * Math.Max(1, level - 1));
    int resultA5 = HealAmountCalculator.ComputeHotTickBase(hot, level, useLevelMinusOne: true);
    bool okA5 = resultA5 == expectedA5;
    Console.WriteLine($"  skill_id={template.SkillId} \"{template.Name}\" (A5, level-1) " +
                      $"result={resultA5} (expected {expectedA5}) {(okA5 ? "PASS" : "FAIL")}");
    if (okA5) healPassed++;
}

// DrainCalculator identity check: HpPercent/MpPercent == 0 always yields 0 gain regardless of caster
// HP/MP state (matches the original per-pool "if (...Percent != 0)" gate collapsing to no-op).
var drainCandidates = allTemplates
    .Where(t => (t.Effects?.DamageEffects?.Count ?? 0) > 0
             && (t.Effects!.DamageEffects[0].HpPercent != 0 || t.Effects!.DamageEffects[0].MpPercent != 0))
    .Take(5)
    .ToList();
Console.WriteLine($"[DrainCalculator] {drainCandidates.Count} candidate skill(s) with drain DamageEffects.");
int drainPassed = 0, drainTotal = 0;
var drainDummyCaster = new Player { MaxHp = 5000, CurrentHp = 4000, MaxMp = 3000, CurrentMp = 2000 };
foreach (var template in drainCandidates)
{
    var fx = template.Effects!.DamageEffects[0];
    drainTotal++;
    var (hpGain, mpGain) = DrainCalculator.Compute(1000, fx, drainDummyCaster);
    int expectedHp = Math.Min(1000 * fx.HpPercent / 100, drainDummyCaster.MaxHp - drainDummyCaster.CurrentHp);
    int expectedMp = Math.Min(1000 * fx.MpPercent / 100, drainDummyCaster.MaxMp - drainDummyCaster.CurrentMp);
    bool ok = hpGain == expectedHp && mpGain == expectedMp;
    Console.WriteLine($"  skill_id={template.SkillId} \"{template.Name}\" hp_gain={hpGain} mp_gain={mpGain} " +
                      $"(expected {expectedHp}/{expectedMp}) {(ok ? "PASS" : "FAIL")}");
    if (ok) drainPassed++;
}

// Full-scan: ComputeInstant/ComputeHotTickBase must never throw for any heal/hot effect on any
// template (Npc target, boostMult 1.0f / 1.5f, level >= 1).
int healScanned = 0, healExceptions = 0;
foreach (var template in allTemplates)
{
    int level = Math.Max(1, template.Level);
    foreach (var he in template.Effects?.HealEffects ?? Array.Empty<SkillHealInfo>())
    {
        healScanned++;
        try { _ = HealAmountCalculator.ComputeInstant(he, MakeNpc(), level, 1.5f); }
        catch (Exception ex)
        {
            healExceptions++;
            Console.WriteLine($"  EXCEPTION skill_id={template.SkillId}: {ex.GetType().Name}: {ex.Message}");
        }
    }
    foreach (var hot in template.Effects?.HotEffects ?? Array.Empty<SkillHotInfo>())
    {
        healScanned++;
        try
        {
            _ = HealAmountCalculator.ComputeHotTickBase(hot, level, useLevelMinusOne: false);
            _ = HealAmountCalculator.ComputeHotTickBase(hot, level, useLevelMinusOne: true);
        }
        catch (Exception ex)
        {
            healExceptions++;
            Console.WriteLine($"  EXCEPTION skill_id={template.SkillId}: {ex.GetType().Name}: {ex.Message}");
        }
    }
}

Console.WriteLine($"HARNESS (HealAmountCalculator): {healPassed}/{healTotal} assertions passed, {healScanned} heal/hot effects scanned, {healExceptions} exceptions");
Console.WriteLine($"HARNESS (DrainCalculator): {drainPassed}/{drainTotal} assertions passed");

// --- SkillDamageCalculator (S4e) ---
// Deterministic sub-formula assertions using a zero-stat dummy Player effector and Npc targets (no
// crit RNG, no resist/dodge rolls, no onetime charges — those stay at the CM_CASTSPELL call sites and
// aren't exercised here). Each "expected" value is computed independently from the documented formula,
// not by re-calling the calculator, so a regression in the calculator body will actually be caught.
var sdEffector = new Player();
int sdPassed = 0, sdTotal = 0;

// ComputeRaw (magical): zero-stat effector + Npc target (Stats == null -> MBResist collapses to 0,
// so suppression and magic-boost-mult are both neutral) reduces to (100 + base) * 1.0f.
{
    const int baseValue = 137;
    int expected = (int)((100 + baseValue) * 1.0f);
    int result = SkillDamageCalculator.ComputeRaw(sdEffector, MakeNpc(), baseValue, isMagical: true,
        applyPassiveSpellAttackBonus: false, onetimeAtkPct: 0);
    sdTotal++;
    bool ok = result == expected;
    Console.WriteLine($"  [SkillDamageCalculator] ComputeRaw(magical) result={result} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) sdPassed++;
}

// ComputeRaw (physical): zero-stat effector reduces pAtk to 0, so raw == baseValue.
{
    const int baseValue = 84;
    int expected = baseValue;
    int result = SkillDamageCalculator.ComputeRaw(sdEffector, MakeNpc(), baseValue, isMagical: false,
        applyPassiveSpellAttackBonus: false, onetimeAtkPct: 0);
    sdTotal++;
    bool ok = result == expected;
    Console.WriteLine($"  [SkillDamageCalculator] ComputeRaw(physical) result={result} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) sdPassed++;
}

// ApplyDefenseAndResist: def>0 mitigation branch (Player target with MagicDefense set).
{
    var defTarget = new Player { MagicDefense = 500 };
    const int raw = 1000, def = 500;
    int expected = Math.Max(1, raw * 1000 / (1000 + def));
    int result = SkillDamageCalculator.ApplyDefenseAndResist(raw, defTarget, isMagical: true,
        hasNoReduce: false, noReduceValue: 0, noReduceIsPercent: false, element: "", applyElementalResist: false);
    sdTotal++;
    bool ok = result == expected;
    Console.WriteLine($"  [SkillDamageCalculator] ApplyDefenseAndResist(def>0) result={result} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) sdPassed++;
}

// ApplyCrit: fixed 1.5x coefficient when useFortitudeCoeff is false (S2/S6), regardless of target.
{
    const int raw = 200;
    int expected = (int)(raw * 1.5f);
    int result = SkillDamageCalculator.ApplyCrit(raw, MakeNpc(), isMagical: true, didCrit: true, useFortitudeCoeff: false);
    sdTotal++;
    bool ok = result == expected;
    Console.WriteLine($"  [SkillDamageCalculator] ApplyCrit(fixed 1.5x) result={result} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) sdPassed++;
}

// ApplyCrit: fortitude coefficient, fort=0 -> max(1, 1.5 - round(0)) = 1.5x.
{
    var fortTarget = new Player { BonusSpellFortitude = 0 };
    const int raw = 200;
    float expectedCoeff = Math.Max(1.0f, 1.5f - (float)Math.Round(0 / 1000.0));
    int expected = (int)(raw * expectedCoeff);
    int result = SkillDamageCalculator.ApplyCrit(raw, fortTarget, isMagical: true, didCrit: true, useFortitudeCoeff: true);
    sdTotal++;
    bool ok = result == expected;
    Console.WriteLine($"  [SkillDamageCalculator] ApplyCrit(fortitude=0) result={result} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) sdPassed++;
}

// ApplyCrit: fortitude coefficient, fort=1000 -> round(1.0)=1 -> max(1, 1.5-1)=1.0x (no bonus, coefficient
// floors at the crit minimum). NOTE: fort=500 is a Math.Round MidpointRounding.ToEven boundary
// (Math.Round(0.5) == 0, verified empirically, NOT 1) so it does NOT give the 1.0x case — using 1000
// instead avoids relying on banker's-rounding trivia to exercise the "coefficient clamped to 1.0" path.
{
    var fortTarget = new Player { BonusSpellFortitude = 1000 };
    const int raw = 200;
    float expectedCoeff = Math.Max(1.0f, 1.5f - (float)Math.Round(1000 / 1000.0));
    int expected = (int)(raw * expectedCoeff);
    int result = SkillDamageCalculator.ApplyCrit(raw, fortTarget, isMagical: true, didCrit: true, useFortitudeCoeff: true);
    sdTotal++;
    bool ok = result == expected;
    Console.WriteLine($"  [SkillDamageCalculator] ApplyCrit(fortitude=1000) result={result} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) sdPassed++;
}

// ApplyDefenseAndResist: elemental resist branch (magical, resist>0, no def so mitigation is a no-op).
{
    var elemTarget = MakeNpc();
    elemTarget.FireResist = 250;
    const int raw = 1000;
    int expected = Math.Max(1, (int)(raw * (1f - 250 / 1250f)));
    int result = SkillDamageCalculator.ApplyDefenseAndResist(raw, elemTarget, isMagical: true,
        hasNoReduce: false, noReduceValue: 0, noReduceIsPercent: false, element: "FIRE", applyElementalResist: true);
    sdTotal++;
    bool ok = result == expected;
    Console.WriteLine($"  [SkillDamageCalculator] ApplyDefenseAndResist(elemental) result={result} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) sdPassed++;
}

// ApplyDefenseAndResist: noreduce override replaces the mitigated value entirely (percent-of-maxhp variant).
{
    var nrTarget = MakeNpc();
    nrTarget.MaxHp = 10_000;
    const int raw = 999_999; // arbitrarily large — must be fully discarded by the override
    int expected = Math.Max(1, nrTarget.MaxHp * 25 / 100);
    int result = SkillDamageCalculator.ApplyDefenseAndResist(raw, nrTarget, isMagical: true,
        hasNoReduce: true, noReduceValue: 25, noReduceIsPercent: true, element: "", applyElementalResist: true);
    sdTotal++;
    bool ok = result == expected;
    Console.WriteLine($"  [SkillDamageCalculator] ApplyDefenseAndResist(noreduce%) result={result} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) sdPassed++;
}

// ApplyLevelDiffAndPvp: Npc branch applies NpcLevelDiffMod when flagged and levelDiff qualifies.
{
    var npcTarget = MakeNpc(); // Level 1 in this harness's MakeNpc()
    var lvlEffector = new Player { Level = 1 };
    // levelDiff = npc.Level(1) - effector.Level(1) = 0 -> NpcLevelDiffMod(0) == 0f -> no-op (raw unchanged)
    const int raw = 400;
    int expected = raw;
    int result = SkillDamageCalculator.ApplyLevelDiffAndPvp(raw, lvlEffector, npcTarget,
        applyNpcLevelDiffMod: true, applyPvp: false);
    sdTotal++;
    bool ok = result == expected;
    Console.WriteLine($"  [SkillDamageCalculator] ApplyLevelDiffAndPvp(levelDiff=0) result={result} (expected {expected}) {(ok ? "PASS" : "FAIL")}");
    if (ok) sdPassed++;
}

Console.WriteLine($"HARNESS (SkillDamageCalculator): {sdPassed}/{sdTotal} assertions passed");

// --- EffectTickScheduler (S5a) ---
// Deterministic scheduler test: drives ProcessDueAsync directly with synthetic timestamps (no real
// PeriodicTimer/host involved), so tick/stop counts are exact and reproducible. Each scenario gets its
// own scheduler + dummy Npc pair so counters can't leak between scenarios.
// Note: Register() stamps NextDueUtc from its own internal DateTime.UtcNow read, which lands a hair
// after this test's captured `now`. JitterMs is a safety margin (comfortably larger than the
// sub-millisecond gap between the two calls) so "now + intervalMs" checks are reliably due without
// making the assertions timing-flaky.
const int JitterMs = 50;

static Npc MakeAliveNpc()
{
    var npc = MakeNpc();
    npc.MaxHp = 100;
    npc.CurrentHp = 100;
    return npc;
}

int schedPassed = 0, schedTotal = 0;
var schedLogger = loggerFactory.CreateLogger<EffectTickScheduler>();

void CheckSched(string label, bool ok)
{
    schedTotal++;
    if (ok) schedPassed++;
    Console.WriteLine($"  [EffectTickScheduler] {label}: {(ok ? "PASS" : "FAIL")}");
}

// Scenario 1: interval ticks fire on every due wake, then EndUtc stops the slot exactly once.
{
    var scheduler = new EffectTickScheduler(schedLogger);
    var effected = MakeAliveNpc();
    var effector = MakeAliveNpc();
    var effect = new AbnormalState { SkillId = 100, Expiry = DateTime.UtcNow.AddSeconds(10) };
    effected.AddEffect(effect);

    int ticks = 0, stops = 0;
    var now = DateTime.UtcNow;
    scheduler.Register(effected, effector, effect, 100, 1000, now.AddMilliseconds(5500 + JitterMs),
        onTick: _ => { ticks++; return ValueTask.CompletedTask; },
        onStop: _ => { stops++; return ValueTask.CompletedTask; });

    foreach (var offsetMs in new[] { 1000, 2000, 3000, 4000, 5000 })
        await scheduler.ProcessDueAsync(now.AddMilliseconds(offsetMs + JitterMs));
    CheckSched("5 due wakes fire exactly 5 onTick calls", ticks == 5 && stops == 0);

    await scheduler.ProcessDueAsync(now.AddMilliseconds(6000 + JitterMs));
    CheckSched("past EndUtc fires onStop exactly once and drops the slot", ticks == 5 && stops == 1);

    await scheduler.ProcessDueAsync(now.AddMilliseconds(7000 + JitterMs));
    CheckSched("dropped slot fires nothing further", ticks == 5 && stops == 1);
}

// Scenario 2: explicit Cancel after 2 ticks stops the slot on the next ProcessDueAsync.
{
    var scheduler = new EffectTickScheduler(schedLogger);
    var effected = MakeAliveNpc();
    var effector = MakeAliveNpc();
    var effect = new AbnormalState { SkillId = 101, Expiry = DateTime.UtcNow.AddSeconds(10) };
    effected.AddEffect(effect);

    int ticks = 0, stops = 0;
    var now = DateTime.UtcNow;
    int handle = scheduler.Register(effected, effector, effect, 101, 1000, now.AddSeconds(10),
        onTick: _ => { ticks++; return ValueTask.CompletedTask; },
        onStop: _ => { stops++; return ValueTask.CompletedTask; });

    await scheduler.ProcessDueAsync(now.AddMilliseconds(1000 + JitterMs));
    await scheduler.ProcessDueAsync(now.AddMilliseconds(2000 + JitterMs));
    CheckSched("2 ticks fired before cancel", ticks == 2 && stops == 0);

    scheduler.Cancel(handle);
    await scheduler.ProcessDueAsync(now.AddMilliseconds(3000 + JitterMs));
    CheckSched("Cancel fires onStop exactly once and no further onTick", ticks == 2 && stops == 1);

    await scheduler.ProcessDueAsync(now.AddMilliseconds(4000 + JitterMs));
    CheckSched("cancelled slot stays dropped", ticks == 2 && stops == 1);
}

// Scenario 3: dispel — the effect is removed from the creature's active-effect list via the real
// Creature API (RemoveEffectBySkillId, which now also calls CancelForEffect — the S5a wiring under
// test). Ticking must not resume regardless of which scheduler guard catches the removal first.
{
    var scheduler = new EffectTickScheduler(schedLogger);
    var effected = MakeAliveNpc();
    var effector = MakeAliveNpc();
    var effect = new AbnormalState { SkillId = 102, Expiry = DateTime.UtcNow.AddSeconds(10) };
    effected.AddEffect(effect);

    int ticks = 0;
    var now = DateTime.UtcNow;
    scheduler.Register(effected, effector, effect, 102, 1000, now.AddSeconds(10),
        onTick: _ => { ticks++; return ValueTask.CompletedTask; });

    await scheduler.ProcessDueAsync(now.AddMilliseconds(1000 + JitterMs));
    CheckSched("1 tick fired before dispel", ticks == 1);

    effected.RemoveEffectBySkillId(102);
    await scheduler.ProcessDueAsync(now.AddMilliseconds(2000 + JitterMs));
    CheckSched("dispel (RemoveEffectBySkillId) stops further ticks", ticks == 1);

    await scheduler.ProcessDueAsync(now.AddMilliseconds(3000 + JitterMs));
    CheckSched("dispelled slot stays dropped", ticks == 1);
}

// Scenario 4: the effected creature dies — the death guard stops the slot on the next ProcessDueAsync.
{
    var scheduler = new EffectTickScheduler(schedLogger);
    var effected = MakeAliveNpc();
    var effector = MakeAliveNpc();
    var effect = new AbnormalState { SkillId = 103, Expiry = DateTime.UtcNow.AddSeconds(10) };
    effected.AddEffect(effect);

    int ticks = 0, stops = 0;
    var now = DateTime.UtcNow;
    scheduler.Register(effected, effector, effect, 103, 1000, now.AddSeconds(10),
        onTick: _ => { ticks++; return ValueTask.CompletedTask; },
        onStop: _ => { stops++; return ValueTask.CompletedTask; });

    await scheduler.ProcessDueAsync(now.AddMilliseconds(1000 + JitterMs));
    CheckSched("1 tick fired before death", ticks == 1 && stops == 0);

    effected.CurrentHp = 0;
    await scheduler.ProcessDueAsync(now.AddMilliseconds(2000 + JitterMs));
    CheckSched("death fires onStop exactly once and drops the slot", ticks == 1 && stops == 1);

    await scheduler.ProcessDueAsync(now.AddMilliseconds(3000 + JitterMs));
    CheckSched("dead-stopped slot stays dropped", ticks == 1 && stops == 1);
}

Console.WriteLine($"HARNESS (EffectTickScheduler): {schedPassed}/{schedTotal} assertions passed");

int totalPassed = passed + debuffPassed + dotPassed + healPassed + drainPassed + sdPassed + schedPassed;
int totalAssertions = total + debuffTotal + dotTotal + healTotal + drainTotal + sdTotal + schedTotal;
int totalExceptions = exceptions + debuffExceptions + dotExceptions + healExceptions;
Console.WriteLine($"HARNESS: {totalPassed}/{totalAssertions} assertions passed, {scanned} templates scanned, {debuffScanned} debuff templates scanned, {dotScanned} dot effects scanned, {healScanned} heal/hot effects scanned, {totalExceptions} exceptions");
return totalPassed == totalAssertions && totalExceptions == 0 ? 0 : 1;
