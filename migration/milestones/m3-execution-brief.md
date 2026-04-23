# M3 — Execution brief

Scope in [`m3-login-gs-handshake.md`](m3-login-gs-handshake.md). **Read [`../agent-rules.md`](../agent-rules.md) first.**

## Preconditions

- M2 closed and passing its smoke.
- `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/network/loginserver/` accessible.

## Reading scope (for agent — see [`../agent-prompt.md`](../agent-prompt.md) tiers)

**Tier A (mandatory):** `agent-rules.md`, `conventions.md`, `m3-login-gs-handshake.md`, this file.

**Tier B — must-read ADRs for M3:**
- [`../adr/001-networking.md`](../adr/001-networking.md) — reused for the GS listener and the outbound LS client.
- [`../adr/002-configuration.md`](../adr/002-configuration.md) — adds shared-secret options and `GameServer:LoginServer:*` section.
- [`../adr/003-hosting.md`](../adr/003-hosting.md) — second listener + `GameServerHost`, new `GsPingService`.
- [`../adr/004-database.md`](../adr/004-database.md) — no new DAOs are expected; reference it only if an unexpected DB need shows up.

**Tier B — step-triggered opens:**
- [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md) — when porting GS↔LS packet classes.

**Do NOT read:** `adr/005-callbacks.md`, `adr/006-scripting.md`, `adr/007-schema-migration.md`, `data-schema.md` — none are needed in M3.

**Tier C — on-demand:**
- [`../risks.md`](../risks.md) — R-001 (ByteBuffer) for packet ports.
- [`../glossary.md`](../glossary.md) — `PeriodicTimer` usage pattern (ping-pong service) if uncertain.

## GS ↔ LS packet inventory (4.6.2 canonical)

Source: `/tmp/aion-refs/4.6.2/AL-Login/src/com/aionemu/loginserver/network/factories/GsPacketHandlerFactory.java`.

GS connection states (LS side): `CONNECTED` → `AUTHED`.

### GS → LS (LS receives these)

| Opcode | State | Class | Purpose |
|---|---|---|---|
| `0x00` | `CONNECTED` | `CM_GS_AUTH` | GS presents id + shared secret |
| `0x0D` | `CONNECTED` | `CM_MAC` | MAC-based checks bootstrap |
| `0x01` | `AUTHED` | `CM_ACCOUNT_AUTH` | Validate session key for a connecting client |
| `0x02` | `AUTHED` | `CM_ACCOUNT_RECONNECT_KEY` | Issue/consume fast-reconnect tokens |
| `0x03` | `AUTHED` | `CM_ACCOUNT_DISCONNECTED` | Client left GS |
| `0x04` | `AUTHED` | `CM_ACCOUNT_LIST` | Sync of currently-connected accounts |
| `0x05` | `AUTHED` | `CM_LS_CONTROL` | LS control commands (kick account) |
| `0x06` | `AUTHED` | `CM_BAN` | Apply bans |
| `0x08` | `AUTHED` | `CM_GS_CHARACTER` | Character metadata sync |
| `0x09` | `AUTHED` | `CM_ACCOUNT_TOLL_INFO` | Cash-shop balance sync |
| `0x0A` | `AUTHED` | `CM_MACBAN_CONTROL` | MAC ban management |
| `0x0B` | `AUTHED` | `CM_PREMIUM_CONTROL` | Premium status update |
| `0x0C` | `AUTHED` | `CM_GS_PONG` | Pong reply |
| `0x0D` | `AUTHED` | `CM_MAC` | MAC update while running |
| `0x0E` | `AUTHED` | `CM_PTRANSFER_CONTROL` | Player transfer between GS |

### LS → GS (LS sends these)

Source: `/tmp/aion-refs/4.6.2/AL-Login/src/com/aionemu/loginserver/network/gameserver/serverpackets/*.java`. Inventory at M3 start via `ls`. Expected set (names verified from class imports):

- `SM_GS_AUTH_RESPONSE` — accept/reject registration.
- `SM_REQUEST_KICK_ACCOUNT` — kick a stuck session.
- `SM_ACCOUNT_AUTH_RESPONSE` — validate answer to `CM_ACCOUNT_AUTH`.
- `SM_ACCOUNT_RECONNECT_KEY` — issued reconnect token.
- `SM_GS_PING` — keep-alive.
- (others per Java source)

## GS-side of the protocol (Java reference, port lives in `AionLightning.Game`)

Source: `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/network/loginserver/`.

The GS holds a **client** connection to LS (outbound), speaks the mirror protocol: receives LS→GS packets, sends GS→LS packets.

## Port order

1. **Shared secrets config**: `appsettings.json` on both LS and GS — `LoginServer:GameServerSharedSecrets:{gsId}` (LS side), `GameServer:LoginServer:SharedSecret` (GS side).
2. **GS models**: `GameServerEntry` record on LS side, stateful `RegisteredGameServer` record tracking live connection.
3. **LS-side GS packet classes** (read first, write second):
   - Read: `CM_GS_AUTH`, `CM_MAC`, `CM_ACCOUNT_AUTH`, `CM_ACCOUNT_RECONNECT_KEY`, `CM_ACCOUNT_DISCONNECTED`, `CM_ACCOUNT_LIST`, `CM_LS_CONTROL`, `CM_BAN`, `CM_GS_CHARACTER`, `CM_ACCOUNT_TOLL_INFO`, `CM_MACBAN_CONTROL`, `CM_PREMIUM_CONTROL`, `CM_GS_PONG`, `CM_PTRANSFER_CONTROL`.
   - Write: `SM_*` set.
4. **`GsConnection`** (LS side) — extends `AConnection`, owns state, ping timer.
5. **`GsConnectionFactory`** — creates connections on accept at port 9014.
6. **Second listener** in `LoginServerHost` — open port `NetworkOptions.GameServerPort`, separate accept loop.
7. **`GameServerTable`** — now state-backed: `ConcurrentDictionary<int, RegisteredGameServer>`. `IsOnline(id)` checks live connection.
8. **`SM_SERVER_LIST`** update — compute `status` from `GameServerTable.IsOnline`.
9. **`SM_PLAY_OK` from M2** — revisit: now returns a real `SessionKey` and GS endpoint from the live `RegisteredGameServer`.
10. **PingPong service** — `BackgroundService` with `PeriodicTimer(Interval=PingPongOptions.DelayMs)`, iterates connected GS, sends ping, expects pong within next interval.
11. **Account handoff path** on LS: when `SM_PLAY_OK` is sent, the account is marked handed-off. When the client reconnects via GS, the GS asks via `CM_ACCOUNT_AUTH` and LS confirms via `SM_ACCOUNT_AUTH_RESPONSE`.
12. **GS side**:
    - `AionLightning.Game/GameServerHost : BackgroundService`.
    - `AionLightning.Game/Network/LoginServer/LsConnection` — outbound connection, sends `CM_GS_AUTH` on connect.
    - `AionLightning.Game/Network/LoginServer/{ClientPackets,ServerPackets}/*.cs` — mirror of LS side.
    - Reconnect with backoff if LS drops.
13. **GS client listener (minimal)** — accept on `NetworkOptions.GameServerPort` (from GS config, separate from LS's port 9014), validate `SessionKey` via `CM_ACCOUNT_AUTH` → `SM_ACCOUNT_AUTH_RESPONSE` roundtrip with LS, then close the client with "character select not implemented yet". Real path — M5.

## GS registration handshake

```
[GS starts]
GS → LS (TCP connect to LS:9014)
  GS→LS: CM_GS_AUTH { gsId, sharedSecret }
  [LS: check secret, check Id not already registered]
  LS→GS: SM_GS_AUTH_RESPONSE { ok? list of client-side endpoints to advertise }

[state on both sides: AUTHED]
  [keep-alive]
  LS→GS: SM_GS_PING   (every PingPongOptions.DelayMs)
  GS→LS: CM_GS_PONG

[client does CM_PLAY on LS]
  LS→GS: — nothing eager; LS simply hands the client off via SM_PLAY_OK with sessionKey + endpoint
  [client TCP connects to GS endpoint]
  GS→LS: CM_ACCOUNT_AUTH { accountId, sessionKey }
  LS→GS: SM_ACCOUNT_AUTH_RESPONSE { valid?, Account payload (name, access_level, membership) }

[client logs out / disconnects from GS]
  GS→LS: CM_ACCOUNT_DISCONNECTED { accountId }
```

## `PeriodicTimer`-based ping service

```csharp
public sealed class GsPingService(
    GameServerTable table,
    IOptions<PingPongOptions> opt,
    ILogger<GsPingService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(opt.Value.DelayMs));
        while (await timer.WaitForNextTickAsync(ct))
        {
            foreach (var gs in table.Registered)
            {
                try { await gs.SendAsync(new SM_GS_PING(), ct); }
                catch (Exception ex) { log.LogWarning(ex, "Ping to GS {Id} failed", gs.Id); }
            }
        }
    }
}
```

## Smoke steps

1. Configure `appsettings.json` on LS:
   ```json
   "LoginServer": {
     "GameServers": [{ "Id": 1, "Name": "AL-Elyos", "HostMask": "127.0.0.1", "Port": 7777, "MaxPlayers": 3000 }],
     "GameServerSharedSecrets": { "1": "local-dev-secret" }
   }
   ```
2. Configure GS:
   ```json
   "GameServer": {
     "Id": 1,
     "LoginServer": { "Host": "127.0.0.1", "Port": 9014, "SharedSecret": "local-dev-secret" },
     "Network": { "BindAddress": "0.0.0.0", "ClientPort": 7777 }
   }
   ```
3. Start LS, wait for "Ready".
4. Start GS. Expected: LS log "GS #1 registered, 0 players". GS log: "Registered with LS".
5. Start Aion client. Log in as `test`. Serverlist: "AL-Elyos" online.
6. Pick server. Client disconnects LS socket and reconnects to GS (127.0.0.1:7777).
7. Expected: GS log "Client connected, session key OK, closing (character select not implemented)".
8. Kill GS process. Within ~1 ping interval, LS log: "GS #1 disconnected". Client reconnects to LS serverlist → shows offline.
9. Restart GS → serverlist flips online again.

## Exit criteria

- All DoD items in [`m3-login-gs-handshake.md`](m3-login-gs-handshake.md) ticked.
- [`../checklist.md`](../checklist.md) M3 section complete.
- Journal entry: `M3 closed: LS ↔ GS handshake live`.
- Active pointer → M4.

## References

- Java LS side: `/tmp/aion-refs/4.6.2/AL-Login/src/com/aionemu/loginserver/network/gameserver/`.
- Java GS side: `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/network/loginserver/`.
- Handler factory: `/tmp/aion-refs/4.6.2/AL-Login/src/com/aionemu/loginserver/network/factories/GsPacketHandlerFactory.java`.
