using AionLightning.Commons.Database.DAO;
using AionLightning.LoginServer.Model;

namespace AionLightning.LoginServer.Dao
{
    public abstract class AccountDAO : IDAO
    {
        public abstract Account GetAccount(string name);
        public abstract Account GetAccount(int id);
        public abstract int GetAccountId(string name);
        public abstract int GetAccountCount();
        public abstract bool InsertAccount(Account account);
        public abstract bool UpdateAccount(Account account);
        public abstract bool UpdateLastIp(int accountId, string ip);
        public abstract bool UpdateLastMac(int accountId, string mac);
        public abstract string GetClassName();
    }
}
