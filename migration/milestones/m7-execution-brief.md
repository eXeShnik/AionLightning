# M7 — Execution brief

Scope in [`m7-verification-harness.md`](m7-verification-harness.md). **Read [`../agent-rules.md`](../agent-rules.md) first.**

## Preconditions

- M5 + at least M6.1–M6.3 closed (so there is functional code worth locking down).
- Every milestone has its own journal closure note — nothing left mid-step.

## Reading scope (for agent — see [`../agent-prompt.md`](../agent-prompt.md) tiers)

**Tier A (mandatory):** `agent-rules.md`, `conventions.md`, `m7-verification-harness.md`, this file.

**Tier B — must-read:**
- [`../adr/004-database.md`](../adr/004-database.md) — DAO integration tests rely on the `MySqlDataSource` contract.
- [`../adr/006-scripting.md`](../adr/006-scripting.md) — the ALC unload test in Step 5 depends on the collectible-ALC contract.

**Tier B — step-triggered opens:**
- [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md) — for codec tests (Step 2).

**Do NOT read:** `adr/001-networking.md`, `adr/002-configuration.md`, `adr/003-hosting.md`, `adr/005-callbacks.md`, `adr/007-schema-migration.md` — not needed for the test harness work.

**Tier C — on-demand:**
- [`../risks.md`](../risks.md) — R-008 (Java server running — affects golden-trace strategy).
- [`../glossary.md`](../glossary.md) — only if a specific Java test pattern is being translated.

## Step order

### Step 1 — Test project scaffolding

Create under `AionLightning.NET/Tests/`:

- `AionLightning.Commons.Tests/` — `xunit`, `FluentAssertions`, `NSubstitute`. Target `net10.0`.
- `AionLightning.Login.Tests/`.
- `AionLightning.Game.Tests/`.
- `AionLightning.Chat.Tests/`.
- `TestSupport/` — common helpers (`PacketBuilder`, `MySqlFixture`, `FakeEventBus`, `FakeScriptHost`).

Add tests projects to `AionLightning.NET.sln`.

### Step 2 — Codec tests (golden bytes)

For each ported packet class in Login (M2), pick a representative byte sequence and store under `Tests/GoldenTraces/<scenario>/<opcode>-<class>.bin`. Example:

```
Tests/GoldenTraces/login-handshake/
├── 0x00-SM_INIT.bin
├── 0x07-CM_AUTH_GG.bin
├── 0x0B-SM_AUTH_GG.bin
├── 0x0B-CM_LOGIN.bin
├── 0x03-SM_LOGIN_OK.bin
├── 0x05-CM_SERVER_LIST.bin
├── 0x04-SM_SERVER_LIST.bin
└── 0x02-CM_PLAY.bin
```

Each test reads the `.bin`, pipes through the .NET packet pipeline, asserts round-trip equality (decode → re-encode → bytes match).

### Step 3 — DAO integration tests

`TestSupport/MySqlFixture.cs` spins up a Testcontainers MySQL 8.4 container, applies `Sql/login/*.sql` via Evolve, exposes connection string.

`AionLightning.Login.Tests/Dao/AccountDaoTests.cs` covers:

- `FindByLoginAsync` happy path and null.
- `CreateAsync` with hashed password.
- `UpdateLastLoginAsync` side effects.
- Banned IP lookup through `BannedIpDao`.

Same pattern for `PlayerDao`, `InventoryDao`.

### Step 4 — Controller unit tests

NSubstitute for DAOs + `AccountController.LoginAsync` scenarios:

- Valid login → `AUTHED`.
- Wrong password → `INVALID_PASSWORD`.
- Banned IP → `BAN_IP`.
- Auto-create on missing account if `AccountsOptions.AutoCreate = true`.
- Maintenance mode → `SERVER_MAINTENANCE`.

### Step 5 — Script-ALC unload test

```csharp
[Fact]
public async Task ReloadingScript_UnloadsPreviousAssembly()
{
    var service = new ScriptService(compiler, watcher, logger);
    await service.LoadAsync("./Scripts/sample");
    var first = WeakReferenceTo(service.Loaded["Greeter"]);
    File.WriteAllText("./Scripts/sample/Greeter.cs", UpdatedGreeterSource);
    await service.ReloadAsync("./Scripts/sample/Greeter.cs");

    // drop strong refs
    service.Unload("Greeter");
    for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); }

    first.TryGetTarget(out _).Should().BeFalse();
}
```

### Step 6 — Golden-trace runner

`TestSupport/GoldenTraceRunner.cs`:

1. Reads a `.bin` as `byte[]`.
2. Feeds it through `AionPacketHandlerFactory` for the matching state (test parameter).
3. Asserts that the resolved packet class matches the expected one.
4. For server packets — reads the expected bytes, constructs the packet from fixture, writes, compares.

Run once per login-handshake sequence.

### Step 7 — CI pipeline

`.github/workflows/ci.yml` (or equivalent):

```yaml
name: CI
on: [push, pull_request]
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore AionLightning.NET/AionLightning.NET.sln
      - run: dotnet build AionLightning.NET/AionLightning.NET.sln --no-restore -c Release /warnaserror
      - run: dotnet test AionLightning.NET/AionLightning.NET.sln --no-build -c Release --logger "trx;LogFileName=test-results.trx"
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: '**/TestResults/*.trx'
```

Testcontainers works in GitHub-hosted runners out of the box (Docker pre-installed on `ubuntu-latest`).

### Step 8 — Test documentation

Append a "Running tests" section to `migration/README.md`:

```
dotnet test AionLightning.NET/AionLightning.NET.sln
```

Add a short "Adding a golden trace" how-to:

1. Capture bytes (either from Java smoke run with Wireshark, or from an existing .NET-side dispatch session).
2. Save under `Tests/GoldenTraces/<scenario>/`.
3. Add a `[Theory]` entry in the matching test class.

## Scoped risks

- Testcontainers in CI requires Docker; if we move to a custom runner, re-verify.
- The MySQL image tag must be pinned (`mysql:8.4`) — floating tags cause flakes.
- ALC unload tests are sensitive to leaked event subscriptions — if they flake, inspect subscription teardown first.

## Exit criteria

- All DoD items in [`m7-verification-harness.md`](m7-verification-harness.md) ticked.
- CI runs green on a trivial PR.
- Coverage on critical paths (auth, packet codec, DAO round-trip, script unload) is real, not smoke.
- Journal entry: `M7 closed: verification harness live`.
