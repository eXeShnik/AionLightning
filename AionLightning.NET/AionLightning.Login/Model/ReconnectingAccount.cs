namespace AionLightning.LoginServer.Model
{
    public class ReconnectingAccount
    {
        public Account Account { get; }
        public int ReconnectionKey { get; }

        public ReconnectingAccount(Account account, int reconnectionKey)
        {
            Account = account;
            ReconnectionKey = reconnectionKey;
        }
    }
}
