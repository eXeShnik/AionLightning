namespace AionLightning.Login.Model;

public sealed class ReconnectingAccount
{
    public Account Account { get; }
    public int ReconnectionKey { get; }

    public ReconnectingAccount(Account account, int reconnectionKey)
    {
        Account = account;
        ReconnectionKey = reconnectionKey;
    }
}
