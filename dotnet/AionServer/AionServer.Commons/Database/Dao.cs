namespace AionServer.Commons.Database;

public interface IDao
{
    bool Supports(string databaseName, int majorVersion, int minorVersion);
}