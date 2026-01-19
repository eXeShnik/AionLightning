namespace AionLightning.Login.Model;

public class AccountTime
{
    public int AccountId { get; set; }
    public DateTime LastLoginTime { get; set; }
    public long SessionDuration { get; set; }
    public long AccumulatedOnlineTime { get; set; }
    public long AccumulatedRestTime { get; set; }
}