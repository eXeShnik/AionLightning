using AionLightning.Login.Model;

namespace AionLightning.Login.Dao;

public abstract class AccountTimeDAO
{
    public abstract AccountTime GetAccountTime(int accountId);
    public abstract bool UpdateAccountTime(AccountTime accountTime);
}