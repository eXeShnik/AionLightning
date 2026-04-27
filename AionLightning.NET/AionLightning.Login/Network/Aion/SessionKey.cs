using AionLightning.Login.Model;

namespace AionLightning.Login.Network.Aion;

public sealed class SessionKey
{
    public int AccountId { get; }
    public int LoginOk { get; }
    public int PlayOk1 { get; }
    public int PlayOk2 { get; }

    public SessionKey(Account account)
    {
        AccountId = account.Id;
        LoginOk = Random.Shared.Next();
        PlayOk1 = Random.Shared.Next();
        PlayOk2 = Random.Shared.Next();
    }

    public SessionKey(int accountId, int loginOk, int playOk1, int playOk2)
    {
        AccountId = accountId;
        LoginOk = loginOk;
        PlayOk1 = playOk1;
        PlayOk2 = playOk2;
    }

    public bool CheckLogin(int accountId, int loginOk) =>
        AccountId == accountId && LoginOk == loginOk;

    public bool CheckSessionKey(SessionKey key) =>
        AccountId == key.AccountId && LoginOk == key.LoginOk &&
        PlayOk1 == key.PlayOk1 && PlayOk2 == key.PlayOk2;
}
