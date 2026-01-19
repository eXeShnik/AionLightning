using System;
using AionLightning.LoginServer.Model;

namespace AionLightning.LoginServer.Network.Aion
{
    public class SessionKey
    {
        public int AccountId { get; }
        public int LoginOk { get; }
        public int PlayOk1 { get; }
        public int PlayOk2 { get; }

        public SessionKey(Account account)
        {
            AccountId = account.Id;
            LoginOk = new Random().Next();
            PlayOk1 = new Random().Next();
            PlayOk2 = new Random().Next();
        }

        public SessionKey(int accountId, int loginOk, int playOk1, int playOk2)
        {
            AccountId = accountId;
            LoginOk = loginOk;
            PlayOk1 = playOk1;
            PlayOk2 = playOk2;
        }

        public bool CheckSessionKey(SessionKey key)
        {
            return AccountId == key.AccountId && LoginOk == key.LoginOk && PlayOk1 == key.PlayOk1 && PlayOk2 == key.PlayOk2;
        }
    }
}
