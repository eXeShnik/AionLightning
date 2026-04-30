using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends the selected target's stats to the requesting player. Opcode 0x29.
/// </summary>
public sealed class SM_TARGET_SELECTED : AionServerPacket
{
    private readonly int _targetObjId;
    private readonly int _level;
    private readonly int _maxHp;
    private readonly int _currentHp;
    private readonly int _maxMp;
    private readonly int _currentMp;

    public SM_TARGET_SELECTED(Player player) : base(0x29)
    {
        if (player.Target is Creature target)
        {
            _targetObjId = target.ObjectId;
            _level       = target switch { Player p => p.Level, Npc n => n.Level, _ => 0 };
            _maxHp       = target.MaxHp;
            _currentHp   = target.CurrentHp;
            _maxMp       = target.MaxMp;
            _currentMp   = target.CurrentMp;
        }
        else if (player.Target is not null)
        {
            _targetObjId = player.Target.ObjectId;
        }
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_targetObjId);
        w.WriteH(_level);
        w.WriteD(_maxHp);
        w.WriteD(_currentHp);
        w.WriteD(_maxMp);
        w.WriteD(_currentMp);
    }
}
