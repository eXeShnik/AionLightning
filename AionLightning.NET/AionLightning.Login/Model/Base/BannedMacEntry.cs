namespace AionLightning.Login.Model.Base;

public sealed class BannedMacEntry
{
    public string Mac { get; }
    public DateTime TimeEnd { get; set; }
    public string Details { get; set; } = string.Empty;

    public BannedMacEntry(string address, long newTime)
    {
        Mac = address;
        UpdateTime(newTime);
    }

    public BannedMacEntry(string address, DateTime time, string details)
    {
        Mac = address;
        TimeEnd = time;
        Details = details;
    }

    public void UpdateTime(long newTime)
    {
        TimeEnd = DateTimeOffset.FromUnixTimeMilliseconds(newTime).UtcDateTime;
    }
}
