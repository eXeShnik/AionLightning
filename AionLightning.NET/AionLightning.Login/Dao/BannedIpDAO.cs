using System;
using System.Collections.Generic;
using AionLightning.Login.Model;

namespace AionLightning.Login.Dao;

public abstract class BannedIpDAO
{
    public abstract ISet<BannedIP> GetAllBans();
    public abstract bool Insert(BannedIP newBan);
    public abstract bool Update(BannedIP ban);
    public abstract bool Delete(string ip);
    public abstract void CleanExpiredBans();
}