using System.Collections.Generic;
using AionLightning.Commons.Database.DAO;
using AionLightning.LoginServer.Model.Base;

namespace AionLightning.LoginServer.Dao
{
    public abstract class BannedMacDAO : IDAO
    {
        public abstract bool Update(BannedMacEntry entry);
        public abstract bool Remove(string address);
        public abstract Dictionary<string, BannedMacEntry> Load();
        public abstract void CleanExpiredBans();
        public abstract bool Insert(BannedMacEntry entry);
        public abstract void Cleanup();

        public string GetClassName()
        {
            return "BannedMacDAO";
        }
    }
}
