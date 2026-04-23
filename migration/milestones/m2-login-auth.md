# M2 — Login server: client auth path

Status: `[ ]` not started · Requires: M1 · ADRs: 001, 002, 003, 004

## Goal

The .NET Login server accepts the Aion 4.6.2 client and carries it through authentication to the serverlist screen, backed by the real `al_server_ls` database.

## Scope

### In

- Client listener on port 2106 built on Pipelines (reuses M1 base classes).
- Packet pipeline: accept → Blowfish init → decode → handle → encode → send.
- Full set of Java `clientpackets` from `AL-Login/src/com/aionemu/loginserver/network/aion/clientpackets/` (4.6.2).
- Full set of Java `serverpackets` from the same package (4.6.2).
- DAOs on Dapper: `AccountDao`, `AccountTimeDao`, `BannedIpDao`, `BannedMacDao`, `PremiumDao`.
- Controllers: `AccountController` (authenticate, update last-login, ban lookup), `BannedIpController`, `BannedMacManager`.
- `SessionKey` generation (used for handoff in M3).
- `GameServerTable` replaced by a config-driven `record[]` in `appsettings.json` (a single stub entry: "No Server Available" state).
- Graceful shutdown: cancellation propagates, all client connections closed, listener stopped.

### Out of scope

- GS listener (port 9014) — M3.
- Any inter-server protocol — M3.
- Auto-create accounts if missing — the config key stays, but only a log line is produced (enabling it is post-M2).
- Premium-account features beyond existence check.

## Dependencies

- Previous: M1 (hosting, config, DB, networking base, Ncrypt).
- ADRs: 001, 002, 003, 004.
- Open questions to close before start: Q-1 (Java 4.6.0 reference — without this we may port 7.8 packets by accident).

## Java source inventory

```bash
ls AL-Login/src/com/aionemu/loginserver/network/aion/clientpackets/*.java
ls AL-Login/src/com/aionemu/loginserver/network/aion/serverpackets/*.java
ls AL-Login/src/com/aionemu/loginserver/controller/*.java
ls AL-Login/src/com/aionemu/loginserver/dao/*.java
ls AL-Login/src/com/aionemu/loginserver/model/*.java
```

The exact packet list is pinned on M2 start. Expect ~10–15 client packets and ~15–20 server packets for the 4.6.2 login flow.

## .NET target layout

### New / rewritten

- `AionLightning.Login/LoginServerHost.cs` — `BackgroundService`. Owns the client listener.
- `AionLightning.Login/Network/Aion/AionConnection.cs` — extends `Commons/Network/AConnection`.
- `AionLightning.Login/Network/Aion/AionConnectionFactory.cs` — per-connection state + Blowfish cipher bootstrap.
- `AionLightning.Login/Network/Aion/ClientPackets/*.cs` — one file per 4.6.2 packet.
- `AionLightning.Login/Network/Aion/ServerPackets/*.cs` — one file per packet.
- `AionLightning.Login/Network/Aion/SessionKey.cs` — already present, reviewed.
- `AionLightning.Login/Network/Factories/AionPacketHandlerFactory.cs` — opcode → packet type lookup.
- `AionLightning.Login/Dao/AccountDao.cs`, `AccountTimeDao.cs`, `BannedIpDao.cs`, `BannedMacDao.cs`, `PremiumDao.cs`.
- `AionLightning.Login/Controller/AccountController.cs`, `BannedIpController.cs`, `BannedMacManager.cs`.
- `AionLightning.Login/Configs/Options/NetworkOptions.cs`, `SecurityOptions.cs`, `AccountsOptions.cs`, `MaintenanceOptions.cs`, `PingPongOptions.cs`, `GameServerEntry.cs`.

### Removed

- `AionLightning.Login/LoginServer.cs` (replaced by `LoginServerHost`).
- `AionLightning.Login/PingPongThread.cs` — replaced by a `PeriodicTimer`-based component in M3 (stub here — do not port Java thread shape).

## Definition of Done

- [ ] 4.6.2 client connects to Login on port 2106 and reaches the serverlist screen.
- [ ] An existing account from the DB logs in successfully, `AccountTime.LastActive` is updated.
- [ ] Wrong password → client shows the correct failure message.
- [ ] A row in `banned_ip` → the client receives the ban response (not a generic failure).
- [ ] Maintenance mode `true` with client GM level < threshold → client sees the maintenance message.
- [ ] Serverlist contains one stub entry marked "Down" or "No Server Available".
- [ ] `Ctrl-C` closes all client connections (observed by logs) and stops the process within 2 seconds.
- [ ] No `NotImplementedException` in any packet handler that is reachable during the smoke flow.

## Smoke scenario

1. Start the MySQL, apply the 4.6.2 schema, insert a test account row (login + the expected hashed password per 4.6.2 format).
2. `dotnet run --project AionLightning.Login`.
3. Launch the Aion 4.6.2 client pointed at `127.0.0.1:2106`.
4. Log in with the test account. Expect: the serverlist screen appears. Expect a DB update to `account_time.last_active`.
5. Log in with a wrong password. Expect: wrong-password dialog on the client.
6. Add `127.0.0.1` into `banned_ip` table. Reconnect. Expect: ban dialog.
7. Flip `Maintenance.Enabled = true` in `appsettings.json`, restart, reconnect with a non-GM account. Expect: maintenance dialog.
8. Press `Ctrl-C`. Expect clean shutdown.

## Scoped risks

- **R-007 (4.6.2 vs 7.8 drift):** the current working branch is `dot_net_10_migration` off `7.8.0`. Enforce the 4.6.0 Java reference before writing a single packet class.
- **Password hash format mismatch:** 4.6.2 password hashing is a known quantity — confirm the algorithm (salt location, encoding) before implementing `AccountController.Authenticate`. See `AL-Login/src/com/aionemu/loginserver/controller/AccountController.java` as source of truth.
- **Blowfish session key timing:** the Aion protocol sends a static-key packet first, then rotates to the session key. Getting the order wrong produces a silent disconnect with nothing in logs — add trace-level hex-dump logging behind a feature flag.

## Notes

- Packet naming: keep the Java names verbatim. E.g. `CM_LOGIN` → file `CM_LOGIN.cs`, class `CM_LOGIN` (suppressing naming warnings inside `Network/Aion/ClientPackets/` namespace is acceptable because the names are protocol identifiers).
- Put an opcode-to-type dictionary in `AionPacketHandlerFactory`; verify with a one-file smoke test that `0x01` resolves to the expected handler.
- Serverlist stub entry shape in `appsettings.json`:
  ```json
  "LoginServer": {
    "GameServers": [
      {
        "Id": 1,
        "Name": "AionLightning",
        "HostMask": "127.0.0.1",
        "Port": 7777,
        "MaxPlayers": 3000
      }
    ]
  }
  ```
