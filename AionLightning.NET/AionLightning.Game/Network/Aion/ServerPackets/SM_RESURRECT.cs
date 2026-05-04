using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Notifies a dead player that another player has cast a resurrection skill on them.
/// Sends the caster's name and skill ID — the client shows a "Do you want to be revived?" dialog.
/// Opcode 0xC2. Mirrors Java SM_RESURRECT.
/// </summary>
public sealed class SM_RESURRECT : AionServerPacket
{
    private readonly string _casterName;
    private readonly int    _skillId;

    public SM_RESURRECT(string casterName, int skillId) : base(0xC2)
    {
        _casterName = casterName;
        _skillId    = skillId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteS(_casterName);
        w.WriteH((short)_skillId);
        w.WriteD(0);
    }
}
