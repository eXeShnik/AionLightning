# M3 — Login ↔ GameServer handshake

Status: `[ ]` not started · Requires: M2 · ADRs: 001, 002, 003, 004

## Goal

The .NET Login server accepts a .NET GameServer skeleton, they complete the inter-server handshake, and a client that picks the registered server gets handed off to the GS socket.

## Scope

### In

- Second listener in Login: port 9014 for GS connections.
- LS-side `GsConnection`, `GsConnectionFactory`, plus all LS-side GS clientpackets / serverpackets from `AL-Login/src/com/aionemu/loginserver/network/gameserver/`.
- GS-side: skeleton `AionLightning.Game` server that opens a socket to Login, authenticates (shared secret from config), registers (id, host-mask, max-players), keeps the connection alive.
- GS-side GS-to-LS packets ported from `AL-Game/src/com/aionemu/gameserver/network/loginserver/` (LS→GS + GS→LS).
- Account handoff: LS generates `SessionKey`, includes it in the "server selected" response; GS validates incoming client with the same key.
- Ping-pong: async `PeriodicTimer`-based keep-alive. Replaces `PingPongThread.cs`.
- Serverlist becomes dynamic: a server shows as "online" only if its GS connection is registered and alive.
- GS-side client listener on its configured port (from LS registration) — accepts connections but, in M3, only performs the session-key check and immediately closes with a placeholder reason. No world entry.

### Out of scope

- Character select screen.
- Any world logic, DAOs for player data.
- Chat.

## Dependencies

- Previous: M1, M2.
- ADRs: 001 (reuses Pipelines infra), 002 (GS options), 003 (GS hosting), 004 (no new DAOs yet).

## Java source inventory

```bash
ls AL-Login/src/com/aionemu/loginserver/network/gameserver/clientpackets/*.java
ls AL-Login/src/com/aionemu/loginserver/network/gameserver/serverpackets/*.java
ls AL-Game/src/com/aionemu/gameserver/network/loginserver/clientpackets/*.java
ls AL-Game/src/com/aionemu/gameserver/network/loginserver/serverpackets/*.java
find AL-Login/src -name "GameServerTable.java" -o -name "GameServerInfo.java"
```

## .NET target layout

### New (Login side)

- `AionLightning.Login/Network/GameServer/GsConnection.cs` (rewritten).
- `AionLightning.Login/Network/GameServer/GsConnectionFactory.cs` (rewritten).
- `AionLightning.Login/Network/GameServer/ClientPackets/*.cs` — GS-to-LS packets.
- `AionLightning.Login/Network/GameServer/ServerPackets/*.cs` — LS-to-GS packets.
- `AionLightning.Login/GameServerTable.cs` — keep as a registry facade, but state comes from live `GsConnection`s, not from the config. Config only seeds the allowed-id list and shared secrets.
- `AionLightning.Login/PingPong/PingPongService.cs` — `PeriodicTimer` inside a `BackgroundService`.

### New (Game side)

- `AionLightning.Game/GameServerHost.cs` — `BackgroundService`.
- `AionLightning.Game/Network/LoginServer/LsConnection.cs` — the GS→LS client connection.
- `AionLightning.Game/Network/LoginServer/ClientPackets/*.cs` — packets GS receives from LS.
- `AionLightning.Game/Network/LoginServer/ServerPackets/*.cs` — packets GS sends to LS.
- `AionLightning.Game/Network/Aion/AionConnection.cs` — the GS-side client connection (minimal in M3).
- `AionLightning.Game/Network/Aion/AionConnectionFactory.cs`.
- `AionLightning.Game/Configs/GameServerOptions.cs`, `LoginServerEndpointOptions.cs`.

### Removed

- `AionLightning.Login/PingPongThread.cs`.

## Definition of Done

- [ ] `dotnet run --project AionLightning.Login` + `dotnet run --project AionLightning.Game` — GS registers with LS; both logs show a successful handshake.
- [ ] Client starts against the real .NET Login, logs in, serverlist shows the registered GS as online.
- [ ] Client picks the server → client reconnects to the GS socket → GS log shows a connection with a valid `SessionKey`.
- [ ] The GS process stops → within 1 ping interval, Login flips the serverlist entry to offline.
- [ ] No `NotImplementedException` on any reachable path during the smoke flow.

## Smoke scenario

1. Start Login (`dotnet run --project AionLightning.Login`). Wait for "Ready".
2. Start GS (`dotnet run --project AionLightning.Game`). Expect Login log: "GS #1 registered, 0 players". Expect GS log: "Registered with LS".
3. Start the Aion client. Log in. Serverlist shows "AionLightning" as online.
4. Pick the server. Client drops the LS socket, connects to GS. GS log: "Client connected, session key OK, closing with placeholder reason".
5. Kill the GS process. Wait one ping interval. Expect Login log: "GS #1 disconnected". Client reconnect → serverlist shows the server offline.

## Scoped risks

- **Opcode drift between 4.6.0 LS and 4.6.0 GS:** the inter-server protocol is symmetric; mismatched opcode tables produce silent failures. Write a compile-time check or a runtime log of unknown opcodes.
- **SessionKey lifetime:** if the key rotates too aggressively, clients get kicked on the handoff. Use the Java TTL as-is.
- **Ping-pong semantics:** `PeriodicTimer` can overlap if a callback takes longer than the period. Guard with a `SemaphoreSlim(1)` or an internal `while (await timer.WaitForNextTickAsync(ct)) { if (Interlocked.Exchange(ref busy, 1) == 1) continue; try { ... } finally { busy = 0; } }` pattern.

## Notes

- The Ncrypt symmetric key between LS and GS is typically a config-held shared secret. Keep it in `appsettings.json` under `LoginServer:GameServerSharedSecrets:[gsId]`. Do not commit real values.
- The GS-side client connection in M3 is intentionally dumb: accept, validate session key, close with "character select not implemented yet". M5 replaces the close with the real character select flow.
