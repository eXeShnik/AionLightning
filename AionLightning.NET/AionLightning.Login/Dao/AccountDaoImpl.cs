using AionLightning.Login.Model;
using Dapper;
using MySqlConnector;

namespace AionLightning.Login.Dao;

public sealed class AccountDaoImpl : IAccountDao
{
    private readonly MySqlDataSource _db;

    public AccountDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<Account?> FindByNameAsync(string name, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Account>(
            """
            SELECT id         AS Id,
                   name       AS Name,
                   password   AS Password,
                   activated  AS Activated,
                   access_level AS AccessLevel,
                   membership AS Membership,
                   last_server AS LastServer,
                   last_ip    AS LastIp,
                   last_mac   AS LastMac,
                   ip_force   AS IpForce,
                   toll       AS Toll
            FROM account_data
            WHERE name = @name
            """, new { name });
    }

    public async Task<int> InsertAsync(Account account, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO account_data (name, password, activated, access_level, membership, last_ip)
            VALUES (@Name, @Password, 1, 0, 0, @LastIp);
            SELECT LAST_INSERT_ID();
            """, account);
    }

    public async Task<bool> UpdateLastIpAsync(int accountId, string ip, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            "UPDATE account_data SET last_ip = @ip WHERE id = @accountId",
            new { ip, accountId });
        return rows > 0;
    }

    public async Task<bool> UpdateLastServerAsync(int accountId, sbyte serverId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            "UPDATE account_data SET last_server = @serverId WHERE id = @accountId",
            new { serverId, accountId });
        return rows > 0;
    }
}
