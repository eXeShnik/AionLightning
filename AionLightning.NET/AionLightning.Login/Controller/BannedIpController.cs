using System.Collections.Generic;
using System.Linq;
using AionLightning.Commons.Utils;
using AionLightning.Login.Dao;
using AionLightning.Login.Model;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Controller;

public class BannedIpController
{
    private readonly ILogger<BannedIpController> _logger;
    private readonly BannedIpDAO _bannedIpDao;
    private ISet<BannedIP> _banList;

    public BannedIpController(ILogger<BannedIpController> logger, BannedIpDAO bannedIpDao)
    {
        _logger = logger;
        _bannedIpDao = bannedIpDao;
        _banList = new HashSet<BannedIP>();
    }

    public void Start()
    {
        Clean();
        Load();
    }

    private void Clean()
    {
        _bannedIpDao.CleanExpiredBans();
    }

    public void Load()
    {
        Reload();
    }

    public void Reload()
    {
        _banList = _bannedIpDao.GetAllBans();
        _logger.LogInformation("BannedIpController loaded {count} IP bans.", _banList.Count);
    }

    public bool IsBanned(string ip)
    {
        return _banList.Any(ipBan => ipBan.IsActive() && NetworkUtils.CheckIPMatching(ipBan.Mask, ip));
    }

    public bool BanIp(string ip)
    {
        return BanIp(ip, null);
    }

    public bool BanIp(string ip, DateTime? time)
    {
        var newBan = new BannedIP { Mask = ip, TimeEnd = time };
        if (_bannedIpDao.Insert(newBan))
        {
            _banList.Add(newBan);
            return true;
        }
        return false;
    }
}