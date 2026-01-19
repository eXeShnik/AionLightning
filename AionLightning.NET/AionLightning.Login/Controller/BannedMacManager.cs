using System.Collections.Generic;
using AionLightning.Commons.Database.DAO;
using AionLightning.LoginServer.Dao;
using AionLightning.LoginServer.Model.Base;

namespace AionLightning.LoginServer.Controller
{
    public class BannedMacManager
    {
        private static readonly BannedMacManager _manager = new BannedMacManager();
        private readonly Dictionary<string, BannedMacEntry> _bannedList;
        private readonly BannedMacDAO _dao;

        public static BannedMacManager GetInstance()
        {
            return _manager;
        }

        private BannedMacManager()
        {
            _dao = DAOManager.GetDAO<BannedMacDAO>();
            _bannedList = _dao.Load();
        }

        public void Unban(string address, string details)
        {
            if (_bannedList.ContainsKey(address))
            {
                _bannedList.Remove(address);
                _dao.Remove(address);
            }
        }

        public void Ban(string address, long time, string details)
        {
            if (_bannedList.ContainsKey(address))
            {
                var entry = _bannedList[address];
                entry.UpdateTime(time);
                entry.Details = details;
                _dao.Update(entry);
            }
            else
            {
                var entry = new BannedMacEntry(address, time) { Details = details };
                _bannedList.Add(address, entry);
                _dao.Insert(entry);
            }
        }

        public bool IsBanned(string address)
        {
            if (_bannedList.ContainsKey(address))
            {
                var entry = _bannedList[address];
                if (entry.TimeEnd.ToUniversalTime() > System.DateTime.UtcNow)
                    return true;

                Unban(address, "Time expired");
            }

            return false;
        }

        public Dictionary<string, BannedMacEntry> GetMap()
        {
            return _bannedList;
        }
    }
}
