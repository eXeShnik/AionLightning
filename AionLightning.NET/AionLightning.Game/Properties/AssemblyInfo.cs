using System.Runtime.CompilerServices;

// S5a: lets AionLightning.Game.EffectHarness drive EffectTickScheduler.ProcessDueAsync directly with
// synthetic timestamps for deterministic scheduler tests without exposing it as a public API.
[assembly: InternalsVisibleTo("AionLightning.Game.EffectHarness")]
