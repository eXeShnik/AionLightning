namespace AionLightning.Commons.Database.DAO
{
    public interface IDAO
    {
        string GetClassName();
        bool Supports(string databaseName, int majorVersion, int minorVersion);
    }
}
