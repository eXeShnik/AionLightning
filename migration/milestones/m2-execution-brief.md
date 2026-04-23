# M2 — Execution brief

Scope, DoD and smoke scenario in [`m2-login-auth.md`](m2-login-auth.md). **Read [`../agent-rules.md`](../agent-rules.md) first.**

## Preconditions

- M1 closed (smoke passes, all Step 11 checks green).
- `/tmp/aion-refs/4.6.0/` accessible.
- Real Aion 4.6.0 client binary available for smoke.

## Reading scope (for agent — see [`../agent-prompt.md`](../agent-prompt.md) tiers)

**Tier A (mandatory):** `agent-rules.md`, `conventions.md`, `m2-login-auth.md`, this file.

**Tier B — must-read ADRs for M2** (open before port order step 1):
- [`../adr/001-networking.md`](../adr/001-networking.md) — packet pipeline, used for the accept loop and `AionConnection`.
- [`../adr/002-configuration.md`](../adr/002-configuration.md) — Login options extended with `GameServerEntry[]`.
- [`../adr/003-hosting.md`](../adr/003-hosting.md) — `LoginServerHost : BackgroundService`.
- [`../adr/004-database.md`](../adr/004-database.md) — all Login DAOs.

**Tier B — step-triggered opens:**
- [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md) — open once when reaching Port Order step 10 (Server packets) and keep referenced through step 11.
- [`../data-schema.md`](../data-schema.md) — open for the `AccountController.EncodePassword` canonical algorithm (port order step 5) and `account_data` columns (port order step 2).

**Do NOT read:** `adr/005-callbacks.md`, `adr/006-scripting.md`, `adr/007-schema-migration.md` — none are used in M2.

**Tier C — on-demand:**
- [`../risks.md`](../risks.md) — R-001 (ByteBuffer) while porting packet classes; R-004 (checked exceptions) during DAO port; R-007 (version drift) — re-read if anything in a Java file seems off vs. the opcode table in this brief.
- [`../glossary.md`](../glossary.md) — keyword grep when unsure about a Java idiom during the port.

## Packet inventory — Aion client ↔ Login (4.6.0 canonical)

Source: `/tmp/aion-refs/4.6.0/AL-Login/src/com/aionemu/loginserver/network/factories/AionPacketHandlerFactory.java`.

Login state machine: `CONNECTED` → `AUTHED_GG` → `AUTHED_LOGIN`.

### Client → Server

| Opcode | State allowed | Class | Purpose |
|---|---|---|---|
| `0x07` | `CONNECTED` | `CM_AUTH_GG` | GameGuard auth (stub in 4.6.0 — accept anything) |
| `0x08` | `CONNECTED` | `CM_UPDATE_SESSION` | Session / Blowfish key rotation |
| `0x0B` | `AUTHED_GG` | `CM_LOGIN` | RSA-encrypted login + password |
| `0x05` | `AUTHED_LOGIN` | `CM_SERVER_LIST` | Request server list |
| `0x02` | `AUTHED_LOGIN` | `CM_PLAY` | Select a server (handoff to GS) |

### Server → Client

Source file: `/tmp/aion-refs/4.6.0/AL-Login/src/com/aionemu/loginserver/network/aion/serverpackets/*.java`.

| Opcode | Class | Triggered by |
|---|---|---|
| `0x00` | `SM_INIT` | Immediately after TCP accept; sends sessionId, RSA public key (scrambled, 0x80 bytes), Blowfish key (0x10 bytes) |
| `0x0B` | `SM_AUTH_GG` | Response to `CM_AUTH_GG` |
| `0x03` | `SM_LOGIN_OK` | Successful `CM_LOGIN` |
| `0x01` | `SM_LOGIN_FAIL` | Failed `CM_LOGIN` |
| `0x04` | `SM_SERVER_LIST` | Response to `CM_SERVER_LIST` |
| `0x07` | `SM_PLAY_OK` | Response to `CM_PLAY` (includes session key for GS) |
| `0x06` | `SM_PLAY_FAIL` | Failed `CM_PLAY` |
| `0x02` | `SM_UPDATE_SESSION` | Response to `CM_UPDATE_SESSION` |

Server opcodes are set via `super(0x00)` etc. in each `SM_*.java` — **verify per class** at port time. The table above is confirmed via `SM_INIT.java` (opcode 0x00) and should be cross-checked in each server-packet port.

## Handshake sequence

```
[TCP connect]
  S→C: SM_INIT (0x00)                         # unencrypted framing bytes, rest Blowfished from next tick
                                              # Fields: sessionId(D), protoRev 0xc621(D), RSA pubkey(128B),
                                              #         zero(16B), blowfishKey(16B), 197635(D), 2097152(D)

[state: CONNECTED]
  C→S: CM_AUTH_GG (0x07)
  S→C: SM_AUTH_GG (0x0B)                      # state -> AUTHED_GG

  or
  C→S: CM_UPDATE_SESSION (0x08)               # early key rotation
  S→C: SM_UPDATE_SESSION (0x02)

[state: AUTHED_GG]
  C→S: CM_LOGIN (0x0B)                        # payload = RSA(login, password) in 128B
  [server: RSA-decrypt, look up account, encode password with SHA-1+Base64, compare]
  S→C: SM_LOGIN_OK (0x03)  or  SM_LOGIN_FAIL (0x01)
                                              # state -> AUTHED_LOGIN on OK

[state: AUTHED_LOGIN]
  C→S: CM_SERVER_LIST (0x05)
  S→C: SM_SERVER_LIST (0x04)                  # hard-coded 1 entry for M2

  C→S: CM_PLAY (0x02) [serverId]
  S→C: SM_PLAY_OK (0x07) [sessionKey]         # or SM_PLAY_FAIL (0x06)
  [server: mark account as handed-off; close after flush]
```

## Port order (topological)

**Do not port packets before the infrastructure that handles them.**

1. **Options records** (`NetworkOptions`, `SecurityOptions`, `AccountsOptions`, `MaintenanceOptions`, `PingPongOptions`) — already created in M1; extend with `GameServerEntry[]` now.
2. **Account model** (`Account` record from `AccountDao`) — maps `account_data` row.
3. **`AccountDao`** — Dapper queries for `FindByLogin`, `Create`, `UpdateLastLogin`, `UpdatePassword`.
4. **`BannedIpDao`, `BannedMacDao`, `AccountTimeDao`, `PremiumDao`** — same pattern.
5. **`AccountController`** — `LoginAsync` method: ban-ip check → find-or-create (per `AccountsOptions.AutoCreate`) → verify password (SHA-1+Base64) → maintenance check → return `AionAuthResponse` enum.
6. **`BannedIpController`, `BannedMacManager`** — in-memory caches of banned entries, reloadable.
7. **`LoginConnection` + state machine** — extends `AConnection`, holds `CryptEngine`, `sessionId`, `Account?`, `SessionKey?`, current `State`.
8. **Crypto bootstrap**:
   - `KeyGen` (RSA + Blowfish key generation) moved to Commons in M1.
   - On connection open: build `EncryptedRSAKeyPair`, send `SM_INIT`, initialise `CryptEngine`.
9. **`AionPacketHandlerFactory`** — opcode → type mapping driven by the state (use the table above).
10. **Server packets** (one `.cs` per Java class): `SM_INIT`, `SM_AUTH_GG`, `SM_LOGIN_OK`, `SM_LOGIN_FAIL`, `SM_SERVER_LIST`, `SM_PLAY_OK`, `SM_PLAY_FAIL`, `SM_UPDATE_SESSION`.
11. **Client packets** (one `.cs` per Java class): `CM_AUTH_GG`, `CM_UPDATE_SESSION`, `CM_LOGIN`, `CM_SERVER_LIST`, `CM_PLAY`.
    - `CM_LOGIN.RunAsync` calls `AccountController.LoginAsync` and responds with `SM_LOGIN_OK` / `SM_LOGIN_FAIL`.
    - `CM_PLAY.RunAsync` generates `SessionKey`, writes `SM_PLAY_OK`, marks `joinedGs = true`.
12. **`LoginServerHost : BackgroundService`** — owns the accept-loop; replaces `LoginServer` + `NetConnector` + `SmokeHost` from M1.
13. **`GameServerTable`** — new: driven by `LoginServer:GameServers` list from `appsettings.json`. In M2, entries have only `{Id, Name, HostMask, Port, MaxPlayers}` and always render as "offline/no GS".

## Password verify implementation

From [`../data-schema.md`](../data-schema.md), in `AccountController`:

```csharp
private static string EncodePassword(string password)
{
    Span<byte> hash = stackalloc byte[20];
    var utf8 = Encoding.UTF8.GetBytes(password);
    SHA1.HashData(utf8, hash);
    return Convert.ToBase64String(hash);
}

public async Task<AionAuthResponse> LoginAsync(string name, string password, LoginConnection conn, CancellationToken ct)
{
    // banned IP check
    if (_bannedIps.IsBanned(conn.RemoteIp))
        return AionAuthResponse.BAN_IP;

    var account = await _accounts.FindByLoginAsync(name, ct);
    if (account is null)
    {
        if (!_accountsOpt.AutoCreate) return AionAuthResponse.INVALID_PASSWORD;
        account = await _accounts.CreateAsync(name, EncodePassword(password), ct);
    }
    else if (account.PasswordHash != EncodePassword(password))
    {
        return AionAuthResponse.INVALID_PASSWORD;
    }

    if (!account.Activated) return AionAuthResponse.BAN_ACCOUNT;
    if (_maintenance.Enabled && account.AccessLevel < _maintenance.GmLevel)
        return AionAuthResponse.SERVER_MAINTENANCE;

    conn.AttachAccount(account);
    await _accountTime.UpdateOnLoginAsync(account.Id, conn.RemoteIp, ct);
    return AionAuthResponse.AUTHED;
}
```

`AionAuthResponse` enum mirrors Java `/tmp/aion-refs/4.6.0/AL-Login/src/com/aionemu/loginserver/network/aion/AionAuthResponse.java` values — **read the Java enum and copy the numeric values exactly**, they land in `SM_LOGIN_FAIL` payload.

## Port template — client packet

Java `/tmp/aion-refs/4.6.0/AL-Login/src/com/aionemu/loginserver/network/aion/clientpackets/CM_LOGIN.java`:

```java
public class CM_LOGIN extends AionClientPacket {
    private byte[] rsaEncrypted = new byte[128];
    @Override
    protected void readImpl() { readB(rsaEncrypted, 0, 128); }
    @Override
    protected void runImpl() {
        LoginConnection client = getConnection();
        byte[] decrypted = KeyGen.decryptRSA(client.getRSAKeyPair().getPair().getPrivate(), rsaEncrypted);
        // bytes 64..78 = login (14 chars), 78..94 = password (16 chars)
        String login = new String(decrypted, 64, 14).trim();
        String password = new String(decrypted, 78, 16).trim();
        AionAuthResponse r = AccountController.login(login, password, client);
        if (r == AionAuthResponse.AUTHED) client.sendPacket(new SM_LOGIN_OK(...));
        else client.sendPacket(new SM_LOGIN_FAIL(r));
    }
}
```

.NET `AionLightning.Login/Network/Aion/ClientPackets/CM_LOGIN.cs`:

```csharp
public sealed class CM_LOGIN(LoginConnection connection, IAccountController accounts) : AionClientPacket
{
    private readonly byte[] _rsaEncrypted = new byte[128];

    public override void Read(ref PacketReader r) => r.ReadB(_rsaEncrypted);

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var decrypted = KeyGen.DecryptRSA(connection.RsaKeyPair.Private, _rsaEncrypted);
        var login = Encoding.ASCII.GetString(decrypted, 64, 14).TrimEnd('\0', ' ');
        var password = Encoding.ASCII.GetString(decrypted, 78, 16).TrimEnd('\0', ' ');

        var result = await accounts.LoginAsync(login, password, connection, ct);
        AionServerPacket response = result == AionAuthResponse.AUTHED
            ? new SM_LOGIN_OK(connection)
            : new SM_LOGIN_FAIL(result);
        await connection.SendAsync(response, ct);
    }
}
```

Pattern holds for all five client packets.

## Port template — server packet

Java `SM_INIT` already included in [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md). Same pattern for all `SM_*`.

## `SM_SERVER_LIST` body shape

Four-byte count, then for each server:

- `writeC(serverId)`
- `writeC(attribute)` — 0x00 normal
- `writeB(hostBytes 4)` — IP octets little-endian
- `writeD(port)`
- `writeC(ageLimit)` — 0
- `writeC(pvpServerMode)` — 0
- `writeH(currentPlayers)`
- `writeH(maxPlayers)`
- `writeC(status)` — 0=down, 1=up

For M2 (no real GS): one entry, status = 0. Java reference: `/tmp/aion-refs/4.6.0/AL-Login/src/com/aionemu/loginserver/network/aion/serverpackets/SM_SERVER_LIST.java` — confirm field order.

## Accept loop

```csharp
public sealed class LoginServerHost(
    IOptions<NetworkOptions> net,
    IServiceProvider sp,
    ILogger<LoginServerHost> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var endpoint = new IPEndPoint(IPAddress.Parse(net.Value.BindAddress), net.Value.ClientPort);
        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(endpoint);
        listener.Listen();
        log.LogInformation("Login accept loop listening on {Endpoint}", endpoint);

        while (!ct.IsCancellationRequested)
        {
            var socket = await listener.AcceptAsync(ct);
            var connFactory = sp.GetRequiredService<IConnectionFactory<LoginConnection>>();
            var connection = connFactory.Create(socket, ct);
            _ = connection.RunAsync(ct); // fire-and-forget per client
        }
    }
}
```

`LoginConnection.RunAsync` sends `SM_INIT` immediately after the crypto engine is bootstrapped, then enters the read-loop that decrypts frames, resolves opcode through `AionPacketHandlerFactory`, and invokes the packet's `RunAsync`.

## Smoke steps

1. Insert a test row into `account_data`:
   ```sql
   INSERT INTO account_data (name, password, activated, access_level)
   VALUES ('test', 'qUqP5cyxm6YcTAhz05Hph5gvu9M=', 1, 0);
   -- SHA-1('test') + Base64 (standard padding) = 'qUqP5cyxm6YcTAhz05Hph5gvu9M='
   ```
2. `dotnet run --project AionLightning.Login`.
3. Launch Aion 4.6.0 client → `127.0.0.1:2106` → login as `test` / `test`.
4. Expect: serverlist with one entry (offline), no crashes.
5. Verify `account_time` updated.
6. Login as `test` / `wrong` → expect correct failure dialog.
7. Insert ban-IP row `INSERT INTO banned_ip (ip) VALUES ('127.0.0.1');`, restart Login, connect → expect ban dialog.
8. Set `LoginServer:Maintenance:Enabled = true` in `appsettings.json`, restart, connect with non-GM account → expect maintenance dialog.

## Exit criteria

- All DoD items in [`m2-login-auth.md`](m2-login-auth.md) ticked.
- [`../checklist.md`](../checklist.md) M2 section marked complete.
- Journal entry: `M2 closed: login auth path live`.
- Move active pointer to M3.

## References

- Java AccountController: `/tmp/aion-refs/4.6.0/AL-Login/src/com/aionemu/loginserver/controller/AccountController.java`.
- Java LoginConnection: `/tmp/aion-refs/4.6.0/AL-Login/src/com/aionemu/loginserver/network/aion/LoginConnection.java`.
- Java client packets: `/tmp/aion-refs/4.6.0/AL-Login/src/com/aionemu/loginserver/network/aion/clientpackets/*.java`.
- Java server packets: `/tmp/aion-refs/4.6.0/AL-Login/src/com/aionemu/loginserver/network/aion/serverpackets/*.java`.
- Schema: [`../data-schema.md`](../data-schema.md) (`account_data` canonical 4.6.0).
- Packet adapter: [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md).
