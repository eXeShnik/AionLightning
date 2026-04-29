using AionLightning.Commons.Utils;
using AionLightning.Login.Dao;
using AionLightning.Login.Model;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Controller;

public sealed class BannedIpController
{
    private readonly ILogger<BannedIpController> _log;
    private readonly IBannedIpDao _dao;
    private volatile ISet<BannedIP> _banList = new HashSet<BannedIP>();

    public BannedIpController(ILogger<BannedIpController> log, IBannedIpDao dao)
    {
        _log = log;
        _dao = dao;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        await _dao.CleanExpiredAsync(ct);
        await ReloadAsync(ct);
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        _banList = await _dao.GetAllAsync(ct);
        _log.LogInformation("BannedIpController loaded {Count} IP bans", _banList.Count);
    }

    public bool IsBanned(string ip) =>
        _banList.Any(b => b.IsActive() && NetworkUtils.CheckIPMatching(b.Mask, ip));

    public async Task<bool> BanIpAsync(string ip, DateTime? until = null, CancellationToken ct = default)
    {
        var ban = new BannedIP { Mask = ip, TimeEnd = until };
        if (!await _dao.InsertAsync(ban, ct)) return false;
        var updated = new HashSet<BannedIP>(_banList) { ban };
        _banList = updated;
        return true;
    }
}
