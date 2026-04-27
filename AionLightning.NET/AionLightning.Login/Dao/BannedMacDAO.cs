using AionLightning.Login.Model.Base;
using System.Collections.Generic;

namespace AionLightning.Login.Dao;

public abstract class BannedMacDAO
{
    public abstract bool Update(BannedMacEntry entry);
    public abstract bool Remove(string address);
    public abstract Dictionary<string, BannedMacEntry> Load();
    public abstract void CleanExpiredBans();
    public abstract bool Insert(BannedMacEntry entry);
    public abstract void Cleanup();
}
