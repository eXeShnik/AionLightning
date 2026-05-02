using AionLightning.Commons.Network;
using AionLightning.Game.DataHolders;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends per-skill cooldown state to the client. Opcode 0x33.</summary>
public sealed class SM_SKILL_COOLDOWN : AionServerPacket
{
    private readonly List<(int SkillId, int RemainingSecs, int BaseMs)> _entries;

    public SM_SKILL_COOLDOWN(SkillData skillData, Dictionary<int, DateTime> skillCooldowns)
        : base(0x33)
    {
        _entries = new List<(int, int, int)>();
        var now = DateTime.UtcNow;
        foreach (var (cdId, expiry) in skillCooldowns)
        {
            if (expiry <= now) continue;
            int remainSecs = (int)(expiry - now).TotalSeconds;
            foreach (var skillId in skillData.GetSkillsForCooldownId(cdId))
            {
                var tmpl = skillData.GetTemplate(skillId);
                if (tmpl is null) continue;
                _entries.Add((skillId, remainSecs, tmpl.Cooldown));
            }
        }
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(_entries.Count);
        w.WriteC(1);
        foreach (var (skillId, remain, baseMs) in _entries)
        {
            w.WriteH(skillId);
            w.WriteD(remain);
            w.WriteD(baseMs);
        }
    }
}
