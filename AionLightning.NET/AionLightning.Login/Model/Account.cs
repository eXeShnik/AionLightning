using AionLightning.Login.Network.Aion;

namespace AionLightning.Login.Model;

public sealed class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public byte AccessLevel { get; set; }
    public byte Membership { get; set; }
    public sbyte Activated { get; set; }
    public DateTime? LastLogin { get; set; }
    public string? LastIp { get; set; }
    public string? LastMac { get; set; }
    public SessionKey? SessionKey { get; set; }
    public global::AionLightning.Login.GameServerInfo? GameServerInfo { get; set; }
    public long? Toll { get; set; }
    public sbyte LastServer { get; set; } = -1;
    public string? IpForce { get; set; }
}
