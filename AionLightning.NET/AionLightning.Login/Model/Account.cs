using System;
using AionLightning.LoginServer.Network.Aion;

namespace AionLightning.LoginServer.Model
{
    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public byte AccessLevel { get; set; }
        public byte Membership { get; set; }
        public sbyte Activated { get; set; }
        public DateTime? LastLogin { get; set; }
        public string LastIp { get; set; }
        public string LastMac { get; set; }
        public SessionKey SessionKey { get; set; }
        public GameServerInfo GameServerInfo { get; set; }
        public int? Toll { get; set; }
    }
}
