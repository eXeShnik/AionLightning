using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Restores the client's item-use cooldown timers, e.g. on login (Java SM_ITEM_COOLDOWN).
/// Opcode 0x67. Per entry: H(delayId) D(secondsLeft) D(useDelaySeconds).
/// </summary>
public sealed class SM_ITEM_COOLDOWN : AionServerPacket
{
    private readonly List<(int DelayId, int SecondsLeft, int DelaySeconds)> _entries;

    public SM_ITEM_COOLDOWN(Dictionary<int, (DateTime Expiry, int DelayMs)> cooldowns) : base(0x67)
    {
        var now = DateTime.UtcNow;
        _entries = cooldowns
            .Where(kv => kv.Value.Expiry > now)
            .Select(kv => (kv.Key,
                (int)(kv.Value.Expiry - now).TotalSeconds,
                kv.Value.DelayMs / 1000))
            .ToList();
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_entries.Count);
        foreach (var (delayId, left, delay) in _entries)
        {
            w.WriteH((short)delayId);
            w.WriteD(left);
            w.WriteD(delay);
        }
    }
}
