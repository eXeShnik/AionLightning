using System;

namespace AionLightning.LoginServer.Model.Base
{
    public class BannedMacEntry
    {
        public string Mac { get; }
        public DateTime TimeEnd { get; set; }
        public string Details { get; set; }

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
            TimeEnd = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(newTime);
        }
    }
}
