# M7 — Verification harness

Status: `[ ]` not started · Requires: core M6 scenarios are working

## Goal

Add the automated test coverage that M1–M6 deliberately deferred. The result is a regression safety net, not comprehensive coverage — we aim to lock down behaviour that already works, not to hit an arbitrary percentage.

## Scope

### In

- xUnit projects:
  - `AionLightning.Commons.Tests` — codec (packet serialize / deserialize) tests, event-bus tests, scripting ALC tests, utility tests.
  - `AionLightning.Login.Tests` — DAO integration tests, controller unit tests, auth-flow integration tests.
  - `AionLightning.Game.Tests` — DAO integration tests, world / region tests, deterministic movement tests.
- Testcontainers-based integration fixtures (MySQL 8.0 container per test class).
- Packet replay harness:
  - Golden-trace binary files under `Tests/GoldenTraces/<scenario>/*.bin` captured manually from the Java server if possible (see R-008), otherwise hand-crafted from Java packet classes.
  - A runner that pipes the client→server bytes through the .NET packet pipeline and asserts the server→client bytes match the golden output.
- CI pipeline (GitHub Actions or equivalent):
  - `dotnet build` with `TreatWarningsAsErrors=true`.
  - `dotnet test` on every PR.
  - MySQL testcontainer pulled for the integration tests.
- Test data helpers:
  - SQL script that seeds a deterministic account / character for tests.
  - A small DSL for composing packets in tests (`Packet.Build(opcode).WriteD(123).WriteS("X")`).

### Out of scope

- 100% coverage — arbitrary target.
- Performance / load tests — separate post-M7 work if ever prioritised.
- End-to-end UI automation with the real client — not feasible without automating NCSOFT's client.

## Dependencies

- Previous: M6 core (at minimum M6.1–M6.3 working).
- All ADRs locked down.

## .NET target layout

```
AionLightning.NET/
├── Tests/
│   ├── AionLightning.Commons.Tests/
│   ├── AionLightning.Login.Tests/
│   ├── AionLightning.Game.Tests/
│   ├── GoldenTraces/
│   │   ├── login-handshake/*.bin
│   │   ├── login-bad-password/*.bin
│   │   └── gs-registration/*.bin
│   └── TestSupport/
│       ├── PacketBuilder.cs
│       ├── MySqlFixture.cs (Testcontainers)
│       └── FakeScriptHost.cs
```

Tests projects target `net10.0` and reference the corresponding production project.

## Definition of Done

- [ ] Every critical path covered by at least one test:
  - Login auth happy path, wrong password, banned IP, maintenance mode.
  - GS registration and disconnect.
  - Chat shout / whisper.
  - World entry + movement broadcast (with a deterministic fake client).
  - Skill cast happy path (if M6.2 landed).
  - Item equip / unequip persistence.
- [ ] Golden-trace runner validates at least the Login handshake and one skill-cast scenario.
- [ ] CI pipeline runs on every PR; merge is blocked when tests fail.
- [ ] README in `migration/` updated with `dotnet test` instructions and a paragraph on how to add a golden trace.
- [ ] Scripting unload is proven in a test: `WeakReference<AssemblyLoadContext>` goes to null after reload + GC.

## Smoke scenario

1. Clean checkout. `dotnet restore && dotnet build` — no errors, no warnings.
2. `dotnet test` — all tests green. Runtime target: under 5 minutes on a laptop.
3. Open a PR with a deliberate breaking change to `AccountController.Authenticate`. Expect CI to fail on the matching unit test before any review.
4. Add a new `clientpacket` with a matching golden-trace file. Test passes. Delete one byte from the trace. Test fails with a clear diff.

## Scoped risks

- **R-008 (Java server not running):** if we cannot capture real traces, golden files are hand-built from Java packet classes — less authoritative, more work.
- **MySQL-version drift between dev and CI:** pin the Testcontainer image tag explicitly (e.g. `mysql:8.4`).
- **Scripting tests require ALC cleanup:** `[Fact]`-based tests can leak assemblies if not careful. Use an xUnit collection fixture that controls the lifecycle explicitly.

## Notes

- M7 is not the end of testing work — it is the *start* of a regression sitting atop finished features. New content in any post-M7 sub-milestone adds tests in the same projects.
- Consider `coverlet` or `dotCover` for coverage reports. Nice to have, not required for DoD.
- Keep test execution fast: the packet codec tests must not hit the network or the DB. Only DAO-integration and end-to-end scenario tests touch Testcontainers.
