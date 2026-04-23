# Database schema

## Sources

Original DDL scripts — `4.6.2` branch, available via the reference worktree:

- `/tmp/aion-refs/4.6.2/AL-Login/sql/al_server_ls.sql` — schema for `al_server_ls` (login database). 169 lines, single file.
- `/tmp/aion-refs/4.6.2/AL-Game/sql/` — schema for the game server (accounts, players, inventory, skills, quests, etc.). Multiple files. Inventoried at the start of M5.

**Never read SQL from `AL-Login/sql/` or `AL-Game/sql/` in the current working tree** — that tree is branch 7.8, not 4.6.2.

## Strategy

1. **Schema ported 1:1.** Table names, column types, FK relationships stay identical to the Java 4.6.2 baseline. No renaming to fit .NET conventions.
2. **SQL files imported verbatim** into `AionLightning.NET/Sql/{login,game}/` and renamed to Evolve's `V{n}__{description}.sql` convention. See [ADR-007](adr/007-schema-migration.md).
3. **Migration tool: Evolve** (SQL-file versioning, `Evolve.Core` NuGet, MIT license).
4. **Connection strings** are MySqlConnector-format. The current `appsettings.json` has `Database.Url = jdbc:mysql://localhost:3306/al_server_ls` — rewrite in M1 (see conversion table below).

## MySQL version

- Recommended: MySQL 8.0+ or MariaDB 10.6+.
- The Aion 4.6.2 schema declares `ENGINE=InnoDB CHARSET=utf8` — compatible with modern MySQL. Do not silently upgrade charset to `utf8mb4` without verifying fields that carry fixed Aion-side encoding.
- Connector: `MySqlConnector` (MIT, genuine async) — see [ADR-004](adr/004-database.md).

## JDBC → MySqlConnector conversion

| JDBC form (current `appsettings.json`) | MySqlConnector form (target) |
|---|---|
| `jdbc:mysql://localhost:3306/al_server_ls` | `Server=localhost;Port=3306;Database=al_server_ls;Uid=root;Pwd=;AllowPublicKeyRetrieval=true;SslMode=None;` |
| `jdbc:mysql://db:3306/al_server_gs?useUnicode=true&characterEncoding=utf8` | `Server=db;Port=3306;Database=al_server_gs;Uid=root;Pwd=<env>;CharSet=utf8;SslMode=Preferred;` |

Always include:

- `AllowPublicKeyRetrieval=true` — MySQL 8 `caching_sha2_password` handshake.
- `SslMode` — `None` for local dev, `Preferred`/`Required` for anything non-local.
- `Default Command Timeout=30` — sane baseline.

Never commit a password. Use `appsettings.Development.json` (gitignored) or environment variables.

## Password storage — 4.6.2 canonical algorithm

Confirmed from `/tmp/aion-refs/4.6.2/AL-Login/src/com/aionemu/loginserver/utils/AccountUtils.java`:

```java
public static String encodePassword(String password) {
    MessageDigest md = MessageDigest.getInstance("SHA-1");
    md.update(password.getBytes("UTF-8"));
    return Base64.encodeToString(md.digest(), false);  // false = no line separators
}
```

.NET equivalent (landing in `AionLightning.Login.Controller.AccountController` in M2):

```csharp
private static string EncodePassword(string password)
{
    Span<byte> hash = stackalloc byte[20]; // SHA1 digest length
    var bytes = Encoding.UTF8.GetBytes(password);
    SHA1.HashData(bytes, hash);
    return Convert.ToBase64String(hash);
}
```

- Algorithm: `SHA-1(UTF-8 bytes(password))`.
- Encoding: standard Base64, **no line separators**, standard padding (`=`). Matches `Convert.ToBase64String` default.
- **No salt.**
- Output length: 28 characters (20 bytes → Base64 → 28 chars including padding). `account_data.password` column is `VARCHAR(65)` — generous headroom.

## `account_data` table (Login DB, canonical 4.6.2)

From `/tmp/aion-refs/4.6.2/AL-Login/sql/al_server_ls.sql`:

```sql
CREATE TABLE `account_data` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(45) NOT NULL,
  `password` varchar(65) NOT NULL,                  -- SHA1+Base64 fits in 28
  `activated` tinyint(1) NOT NULL DEFAULT '1',
  `access_level` tinyint(3) NOT NULL DEFAULT '0',   -- 0 = player, >0 = GM
  `membership` tinyint(3) NOT NULL DEFAULT '0',
  `old_membership` tinyint(3) NOT NULL DEFAULT '0',
  `last_server` tinyint(3) NOT NULL DEFAULT '-1',
  `last_ip` varchar(20) DEFAULT NULL,
  `last_mac` varchar(20) NOT NULL DEFAULT 'xx-xx-xx-xx-xx-xx',
  `ip_force` varchar(20) DEFAULT NULL,
  `expire` date DEFAULT NULL,                       -- account expiry
  `toll` bigint(13) NOT NULL DEFAULT '0',           -- cash shop currency
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB CHARSET=utf8;
```

Other tables in the same file (inventory done at M2 start):

- `account_rewards`
- `account_time` (login history)
- `banned_ip`
- `banned_mac`
- `gameserver_list` (registered GS metadata)
- `premium_list`
- `reconnect_data` (fast-reconnect tokens)
- `toll_info`

## Main table groups

| Group | Database | Contents (verify against `/tmp/aion-refs/4.6.2/AL-Game/sql/`) |
|---|---|---|
| Accounts | login DB | `account_data`, `account_time`, `account_rewards`, `banned_ip`, `banned_mac`, `premium_list`, `reconnect_data`, `toll_info`, `gameserver_list` |
| Players | game DB | `players`, `player_appearance`, `player_skills`, `player_abyss_rank`, `player_macrosses`, `player_titles`, `player_settings` |
| Inventory | game DB | `inventory`, `warehouse`, `legion_warehouse`, `player_cubes` |
| Quests | game DB | `player_quests` |
| Skills | game DB | `player_skills`, `skill_cooldowns` |
| Legions | game DB | `legions`, `legion_members`, `legion_emblems`, `legion_history` |
| Instances | game DB | `instance_cooldown`, `instance_data` |
| Mail | game DB | `mail` |
| Friends | game DB | `friends`, `blocks` |

Exact inventory is pinned in M2 (Login DAOs) and at the start of every M6 sub-milestone.

## DAO conventions

- All DAOs use Dapper ([ADR-004](adr/004-database.md)).
- Methods are async with `CancellationToken`.
- Parameters are named (`@accountId`), never interpolated into SQL.
- Return `record` DTOs, not entity classes (at this layer).
- Mapping into domain models is a separate layer (a mapper class or `ToDomain` extension).

## Connection string example (target for M1)

```json
{
  "ConnectionStrings": {
    "LoginDb": "Server=localhost;Port=3306;Database=al_server_ls;Uid=root;Pwd=;AllowPublicKeyRetrieval=true;SslMode=None;Default Command Timeout=30;",
    "GameDb":  "Server=localhost;Port=3306;Database=al_server_gs;Uid=root;Pwd=;AllowPublicKeyRetrieval=true;SslMode=None;Default Command Timeout=30;CharSet=utf8;"
  }
}
```

Binding:

```csharp
builder.Services.AddMySqlDataSource(
    builder.Configuration.GetConnectionString("LoginDb")!,
    serviceKey: "login");
```

## Evolve startup (landing in M1)

```csharp
using (var conn = new MySqlConnection(builder.Configuration.GetConnectionString("LoginDb")))
{
    var evolve = new Evolve.Evolve(conn, msg => logger.LogInformation("Evolve: {Message}", msg))
    {
        Locations = new[] { "Sql/login" },
        IsEraseDisabled = true,
        MetadataTableName = "_evolve_changelog",
    };
    evolve.Migrate();
}
```

## Resolved items

- **Seed data.** Most static content (NPC / item / skill templates) is in XML under `AL-Game/data/`, not in SQL. Only reference rows (e.g. GM default account) might be seeded via SQL. Add as `V2__seed_dev.sql` per environment if needed.
- **Charset.** `accounts.charset=ISO8859_2` in the Java `appsettings.json` is a legacy remnant; the Java code always encodes passwords as UTF-8 (confirmed above). The config key is preserved for reference but unused.
- **Password storage.** Resolved above — SHA-1 + Base64, no salt.
