using AionLightning.Commons.Network;
using AionLightning.Game.Model.Skill;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_SKILL_LIST : AionServerPacket
{
    private readonly IReadOnlyList<PlayerSkillEntry> _skills;
    private readonly bool   _isNew;
    private readonly int    _msgId;
    private readonly string _skillName;
    private readonly int    _skillLevel;

    public SM_SKILL_LIST(IEnumerable<PlayerSkillEntry> skills, bool isNew = true,
        int msgId = 0, string skillName = "", int skillLevel = 0)
        : base(0x2C)
    {
        _skills     = skills.ToList();
        _isNew      = isNew;
        _msgId      = msgId;
        _skillName  = skillName;
        _skillLevel = skillLevel;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH((short)_skills.Count);
        w.WriteC(_isNew ? (byte)1 : (byte)0);

        int learnedAt = _isNew ? (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() : 0;
        foreach (var s in _skills)
        {
            w.WriteH((short)s.SkillId);
            w.WriteH((short)s.SkillLevel);
            w.WriteC(0);
            w.WriteC(0);           // extraLvl — 0 for all non-crafting skills
            w.WriteD(learnedAt);
            w.WriteC(s.IsStigma ? (byte)1 : (byte)0);
        }

        w.WriteD(_msgId);
        if (_msgId != 0)
        {
            w.WriteS(_skillName);
            w.WriteH((short)_skillLevel);
        }
    }
}
