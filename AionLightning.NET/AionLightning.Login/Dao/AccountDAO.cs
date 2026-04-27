using AionLightning.Login.Model;

namespace AionLightning.Login.Dao;

public abstract class AccountDAO
{
    public abstract Account? GetAccount(string name);
    public abstract Account? GetAccount(int id);
    public abstract int GetAccountId(string name);
    public abstract int GetAccountCount();
    public abstract bool InsertAccount(Account account);
    public abstract bool UpdateAccount(Account account);
    public abstract bool UpdateLastIp(int accountId, string ip);
    public abstract bool UpdateLastMac(int accountId, string mac);
}
