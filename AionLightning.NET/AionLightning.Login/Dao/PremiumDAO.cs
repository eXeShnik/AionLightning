namespace AionLightning.Login.Dao;

public abstract class PremiumDAO
{
    public abstract long GetPoints(int accountId);
    public abstract bool UpdatePoints(int accountId, long points, long required);
}