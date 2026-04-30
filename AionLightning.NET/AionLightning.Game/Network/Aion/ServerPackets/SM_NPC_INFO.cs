using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Introduces a visible NPC to the client. Mirrors Java SM_NPC_INFO.
/// Sends minimal data: no equipment, no AI targeting, default speed/radius.
/// </summary>
public sealed class SM_NPC_INFO : AionServerPacket
{
    private readonly Npc _npc;

    public SM_NPC_INFO(Npc npc) : base(0x0E) => _npc = npc;

    public override void Write(ref PacketWriter w)
    {
        var pos = _npc.Position;
        var tpl = _npc.Template;

        w.WriteF(pos.X);
        w.WriteF(pos.Y);
        w.WriteF(pos.Z);
        w.WriteD(_npc.ObjectId);
        w.WriteD(tpl.NpcId);
        w.WriteD(tpl.NpcId); // repeated per protocol

        // npcTypeId (CreatureType): 0=ATTACKABLE, 2=PEACE
        byte npcType = tpl.Ai.Equals("dummy", StringComparison.OrdinalIgnoreCase) ? (byte)2 : (byte)0;
        w.WriteC(npcType);

        w.WriteH(65);  // state: 65 = normal standing
        w.WriteC((byte)pos.Heading);

        w.WriteD(tpl.NameId);
        w.WriteD(0); // titleId

        w.WriteH(0); // unk
        w.WriteC(0); // unk
        w.WriteD(0); // unk

        w.WriteD(0);   // creatorId (no master)
        w.WriteS("");  // masterName (empty)

        int maxHp  = _npc.MaxHp > 0 ? _npc.MaxHp : 1;
        int currHp = _npc.CurrentHp > 0 ? _npc.CurrentHp : maxHp;
        w.WriteC((byte)(100 * currHp / maxHp)); // hp%
        w.WriteD(maxHp);
        w.WriteC(tpl.Level);

        w.WriteD(0);    // no gear mask → no equipment entries
        w.WriteF(1.0f); // bound radius front

        w.WriteF(tpl.Height > 0 ? tpl.Height : 1.0f);
        w.WriteF(0.3f); // movement speed

        w.WriteH(1500); // attack delay
        w.WriteH(1500); // attack delay (repeated)

        w.WriteC(0); // not a flag, not a new spawn

        // movement target (none)
        w.WriteF(0f);
        w.WriteF(0f);
        w.WriteF(0f);
        w.WriteC(0); // movement mask

        w.WriteH(0); // static spawn id

        // 8 unknown bytes
        w.WriteC(0); w.WriteC(0); w.WriteC(0); w.WriteC(0);
        w.WriteC(0); w.WriteC(0); w.WriteC(0); w.WriteC(0);

        w.WriteC(0);  // visualState
        w.WriteH(1);  // NpcObjectType: 1 = NORMAL
        w.WriteC(0);  // unk
        w.WriteD(0);  // targetObjectId (no target)
        w.WriteD(0);  // townId
    }
}
