using AionServer.Login.Database;
using AionServer.Login.Models;
using Microsoft.Extensions.Configuration;

namespace AionServer.Login.Scripts.Database;

public class MySQL5AccountDao : AccountDao
{
    public MySQL5AccountDao(IConfiguration configuration)
    {
        
    }

    public override Account? GetAccount(string name)
    {
        throw new NotImplementedException();
    }

    public override Account? GetAccount(int id)
    {
        throw new NotImplementedException();
    }

    public override int GetAccountId(string name)
    {
        throw new NotImplementedException();
    }

    public override int GetAccountCount()
    {
        throw new NotImplementedException();
    }

    public override bool InsertAccount(Account account)
    {
        throw new NotImplementedException();
    }

    public override bool UpdateAccount(Account account)
    {
        throw new NotImplementedException();
    }

    public override bool UpdateLastServer(int accountId, byte lastServer)
    {
        throw new NotImplementedException();
    }

    public override bool UpdateLastIp(int accountId, string ip)
    {
        throw new NotImplementedException();
    }

    public override string? GetLastIp(int accountId)
    {
        throw new NotImplementedException();
    }

    public override bool UpdateLastMac(int accountId, string mac)
    {
        throw new NotImplementedException();
    }

    public override bool UpdateMembership(int accountId)
    {
        throw new NotImplementedException();
    }

    public override void DeleteInactiveAccounts(int daysOfInactivity)
    {
        throw new NotImplementedException();
    }
}