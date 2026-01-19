using System;

namespace AionLightning.Login.Model;

public class BannedIP
{
    public int? Id { get; set; }
    public string Mask { get; set; }
    public DateTime? TimeEnd { get; set; }

    public bool IsActive()
    {
        return TimeEnd == null || TimeEnd > DateTime.UtcNow;
    }
}